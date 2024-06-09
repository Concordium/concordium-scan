use anyhow::Context as _;
use async_graphql::{
    types::{
        self,
        connection,
    },
    ComplexObject,
    Context,
    Enum,
    InputObject,
    InputValueError,
    InputValueResult,
    Interface,
    Number,
    Object,
    Scalar,
    ScalarType,
    SimpleObject,
    Union,
    Value,
};
use chrono::Duration;
use futures::prelude::*;
use sqlx::{
    postgres::types::PgInterval,
    PgPool,
    Postgres,
};
use std::{
    error::Error,
    sync::Arc,
};

const VERSION: &str = env!("CARGO_PKG_VERSION");

#[derive(Debug, thiserror::Error, Clone)]
enum ApiError {
    #[error("Could not find resource")]
    NotFound,
    #[error("Internal error: {}", .0.message)]
    NoDatabasePool(async_graphql::Error),
    #[error("Internal error: {0}")]
    FailedDatabaseQuery(Arc<sqlx::Error>),
    #[error("Invalid ID format: {0}")]
    InvalidIdInt(std::num::ParseIntError),
    #[error("Invalid ID format: {0}")]
    InvalidIdIntSize(std::num::TryFromIntError),
    #[error("Invalid ID for transaction, must be of the format 'block:index'")]
    InvalidIdTransaction,
    #[error("The period cannot be converted")]
    DurationOutOfRange(Arc<Box<dyn Error + Send + Sync>>),
    #[error("The \"first\" and \"last\" parameters cannot exist at the same time")]
    QueryConnectionFirstLast,
    #[error("The \"first\" parameter must be a non-negative number")]
    QueryConnectionNegativeFirst,
    #[error("The \"last\" parameter must be a non-negative number")]
    QueryConnectionNegativeLast,
    #[error("Internal error: {0}")]
    InternalError(String),
    #[error("Invalid integer: {0}")]
    InvalidInt(#[from] std::num::TryFromIntError),
}

impl From<sqlx::Error> for ApiError {
    fn from(value: sqlx::Error) -> Self {
        ApiError::FailedDatabaseQuery(Arc::new(value))
    }
}

type ApiResult<A> = Result<A, ApiError>;

fn get_pool<'a>(ctx: &Context<'a>) -> ApiResult<&'a PgPool> {
    ctx.data::<PgPool>().map_err(ApiError::NoDatabasePool)
}

fn check_connection_query(first: &Option<i64>, last: &Option<i64>) -> ApiResult<()> {
    if first.is_some() && last.is_some() {
        return Err(ApiError::QueryConnectionFirstLast);
    }
    if let Some(first) = first {
        if first < &0 {
            return Err(ApiError::QueryConnectionNegativeFirst);
        }
    };
    if let Some(last) = last {
        if last < &0 {
            return Err(ApiError::QueryConnectionNegativeLast);
        }
    };
    Ok(())
}

pub struct Query;
#[Object]
impl Query {
    async fn versions(&self) -> Versions {
        Versions {
            backend_versions: VERSION.to_string(),
        }
    }
    async fn block<'a>(&self, ctx: &Context<'a>, height_id: types::ID) -> ApiResult<Block> {
        let height: i64 = height_id
            .clone()
            .try_into()
            .map_err(ApiError::InvalidIdInt)?;
        sqlx::query_as!(Block, "SELECT * FROM blocks WHERE height=$1", height)
            .fetch_optional(get_pool(ctx)?)
            .await?
            .ok_or(ApiError::NotFound)
    }
    async fn block_by_block_hash<'a>(
        &self,
        ctx: &Context<'a>,
        block_hash: BlockHash,
    ) -> ApiResult<Block> {
        sqlx::query_as!(Block, "SELECT * FROM blocks WHERE hash=$1", block_hash)
            .fetch_optional(get_pool(ctx)?)
            .await?
            .ok_or(ApiError::NotFound)
    }

    async fn blocks<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<i64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<i64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Block>> {
        check_connection_query(&first, &last)?;

        let mut builder =
            sqlx::QueryBuilder::<'_, Postgres>::new("SELECT * FROM (SELECT * FROM blocks");

        match (after, before) {
            (None, None) => {},
            (None, Some(before)) => {
                builder
                    .push(" WHERE height < ")
                    .push_bind(before.parse::<i64>().map_err(ApiError::InvalidIdInt)?);
            },
            (Some(after), None) => {
                builder
                    .push(" WHERE height > ")
                    .push_bind(after.parse::<i64>().map_err(ApiError::InvalidIdInt)?);
            },
            (Some(after), Some(before)) => {
                builder
                    .push(" WHERE height > ")
                    .push_bind(after.parse::<i64>().map_err(ApiError::InvalidIdInt)?)
                    .push(" AND height < ")
                    .push_bind(before.parse::<i64>().map_err(ApiError::InvalidIdInt)?);
            },
        }

        match (first, &last) {
            (None, None) => {
                builder.push(" ORDER BY height ASC)");
            },
            (None, Some(last)) => {
                builder
                    .push(" ORDER BY height DESC LIMIT ")
                    .push_bind(last)
                    .push(") ORDER BY height ASC ");
            },
            (Some(first), None) => {
                builder
                    .push(" ORDER BY height ASC LIMIT ")
                    .push_bind(first)
                    .push(")");
            },
            (Some(_), Some(_)) => return Err(ApiError::QueryConnectionFirstLast),
        }

        let mut block_stream = builder.build_query_as::<Block>().fetch(get_pool(ctx)?);

        let mut connection = connection::Connection::new(true, true);
        while let Some(block) = block_stream.try_next().await? {
            connection
                .edges
                .push(connection::Edge::new(block.height.to_string(), block));
        }
        if last.is_some() {
            if let Some(edge) = connection.edges.last() {
                connection.has_previous_page = edge.node.height != 0;
            }
        } else {
            if let Some(edge) = connection.edges.first() {
                connection.has_previous_page = edge.node.height != 0;
            }
        }

        Ok(connection)
    }

    async fn transaction<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Transaction> {
        let id = IdTransaction::try_from(id)?;
        sqlx::query_as("SELECT * FROM transactions WHERE block=$1 AND index=$2")
            .bind(id.block)
            .bind(id.index)
            .fetch_optional(get_pool(ctx)?)
            .await?
            .ok_or(ApiError::NotFound)
    }
    async fn transaction_by_transaction_hash<'a>(
        &self,
        ctx: &Context<'a>,
        transaction_hash: TransactionHash,
    ) -> ApiResult<Transaction> {
        sqlx::query_as("SELECT * FROM transactions WHERE hash=$1")
            .bind(transaction_hash)
            .fetch_optional(get_pool(ctx)?)
            .await?
            .ok_or(ApiError::NotFound)
    }
    async fn transactions(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        todo!()
    }
    async fn account<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Account> {
        let index: i64 = id.clone().try_into().map_err(ApiError::InvalidIdInt)?;
        sqlx::query_as("SELECT * FROM accounts WHERE index=$1")
            .bind(index)
            .fetch_optional(get_pool(ctx)?)
            .await?
            .ok_or(ApiError::NotFound)
    }
    async fn account_by_address<'a>(
        &self,
        ctx: &Context<'a>,
        account_address: String,
    ) -> ApiResult<Account> {
        sqlx::query_as("SELECT * FROM accounts WHERE address=$1")
            .bind(account_address)
            .fetch_optional(get_pool(ctx)?)
            .await?
            .ok_or(ApiError::NotFound)
    }
    async fn accounts<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(default)] _sort: AccountSort,
        _filter: Option<AccountFilterInput>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<i64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<i64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Account>> {
        check_connection_query(&first, &last)?;

        let mut builder =
            sqlx::QueryBuilder::<'_, Postgres>::new("SELECT * FROM (SELECT * FROM accounts");

        // TODO: include sort and filter

        match (after, before) {
            (None, None) => {},
            (None, Some(before)) => {
                builder
                    .push(" WHERE index < ")
                    .push_bind(before.parse::<i64>().map_err(ApiError::InvalidIdInt)?);
            },
            (Some(after), None) => {
                builder
                    .push(" WHERE index > ")
                    .push_bind(after.parse::<i64>().map_err(ApiError::InvalidIdInt)?);
            },
            (Some(after), Some(before)) => {
                builder
                    .push(" WHERE index > ")
                    .push_bind(after.parse::<i64>().map_err(ApiError::InvalidIdInt)?)
                    .push(" AND index < ")
                    .push_bind(before.parse::<i64>().map_err(ApiError::InvalidIdInt)?);
            },
        }

        match (first, &last) {
            (None, None) => {
                builder.push(" ORDER BY index ASC)");
            },
            (None, Some(last)) => {
                builder
                    .push(" ORDER BY index DESC LIMIT ")
                    .push_bind(last)
                    .push(") ORDER BY index ASC ");
            },
            (Some(first), None) => {
                builder
                    .push(" ORDER BY index ASC LIMIT ")
                    .push_bind(first)
                    .push(")");
            },
            (Some(_), Some(_)) => return Err(ApiError::QueryConnectionFirstLast),
        }

        let mut row_stream = builder.build_query_as::<Account>().fetch(get_pool(ctx)?);

        let mut connection = connection::Connection::new(true, true);
        while let Some(row) = row_stream.try_next().await? {
            connection
                .edges
                .push(connection::Edge::new(row.index.to_string(), row));
        }
        if last.is_some() {
            if let Some(edge) = connection.edges.last() {
                connection.has_previous_page = edge.node.index != 0;
            }
        } else {
            if let Some(edge) = connection.edges.first() {
                connection.has_previous_page = edge.node.index != 0;
            }
        }

        Ok(connection)
    }
    async fn baker(&self, _id: types::ID) -> Baker {
        todo!()
    }
    async fn baker_by_baker_id(&self, _id: BakerId) -> Baker {
        todo!()
    }

    async fn bakers(
        &self,
        #[graphql(default)] _sort: BakerSort,
        _filter: BakerFilterInput,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        todo!()
    }

    async fn search(&self, query: String) -> SearchResult {
        SearchResult { _query: query }
    }
    async fn block_metrics<'a>(
        &self,
        ctx: &Context<'a>,
        period: MetricsPeriod,
    ) -> ApiResult<BlockMetrics> {
        let pool = get_pool(ctx)?;

        let queried_period: Duration = match period {
            MetricsPeriod::LastHour => Duration::hours(1),
            MetricsPeriod::Last24Hours => Duration::hours(24),
            MetricsPeriod::Last7Days => Duration::days(7),
            MetricsPeriod::Last30Days => Duration::days(30),
            MetricsPeriod::LastYear => Duration::days(364),
        };

        let interval: PgInterval = queried_period
            .try_into()
            .map_err(|err| ApiError::DurationOutOfRange(Arc::new(err)))?;
        let rec = sqlx::query!(
            "SELECT
MAX(height) as last_block_height,
COUNT(1) as blocks_added,
(MAX(slot_time) - MIN(slot_time)) / (COUNT(1) - 1) as avg_block_time
FROM blocks
WHERE slot_time > (LOCALTIMESTAMP - $1::interval)",
            interval
        )
        .fetch_one(pool)
        .await?;

        Ok(BlockMetrics {
            last_block_height: rec.last_block_height.unwrap_or(0),
            blocks_added: rec.blocks_added.unwrap_or(0),
            avg_block_time: rec.avg_block_time.map(|i| i.microseconds as f64),
            // TODO check what format this is expected to be in.
        })
    }

    // accountsMetrics(period: MetricsPeriod!): AccountsMetrics
    // transactionMetrics(period: MetricsPeriod!): TransactionMetrics
    // bakerMetrics(period: MetricsPeriod!): BakerMetrics!
    // rewardMetrics(period: MetricsPeriod!): RewardMetrics!
    // rewardMetricsForAccount(accountId: ID! period: MetricsPeriod!): RewardMetrics!
    // poolRewardMetricsForPassiveDelegation(period: MetricsPeriod!): PoolRewardMetrics!
    // poolRewardMetricsForBakerPool(bakerId: ID! period: MetricsPeriod!): PoolRewardMetrics!
    // passiveDelegation: PassiveDelegation
    // paydayStatus: PaydayStatus
    // latestChainParameters: ChainParameters
    // importState: ImportState
    // nodeStatuses(sortField: NodeSortField! sortDirection: NodeSortDirection! "Returns the first
    // _n_ elements from the list." first: Int "Returns the elements in the list that come after the
    // specified cursor." after: String "Returns the last _n_ elements from the list." last: Int
    // "Returns the elements in the list that come before the specified cursor." before: String):
    // NodeStatusesConnection nodeStatus(id: ID!): NodeStatus
    // tokens("Returns the first _n_ elements from the list." first: Int "Returns the elements in
    // the list that come after the specified cursor." after: String "Returns the last _n_ elements
    // from the list." last: Int "Returns the elements in the list that come before the specified
    // cursor." before: String): TokensConnection token(contractIndex: UnsignedLong!
    // contractSubIndex: UnsignedLong! tokenId: String!): Token! contract(contractAddressIndex:
    // UnsignedLong! contractAddressSubIndex: UnsignedLong!): Contract contracts("Returns the
    // first _n_ elements from the list." first: Int "Returns the elements in the list that come
    // after the specified cursor." after: String "Returns the last _n_ elements from the list."
    // last: Int "Returns the elements in the list that come before the specified cursor." before:
    // String): ContractsConnection moduleReferenceEvent(moduleReference: String!):
    // ModuleReferenceEvent
}

/// The UnsignedLong scalar type represents a unsigned 64-bit numeric non-fractional value greater
/// than or equal to 0.
#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
struct UnsignedLong(u64);
#[Scalar]
impl ScalarType for UnsignedLong {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_u64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value {
        Value::Number(self.0.into())
    }
}

/// The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can
/// represent values between -(2^63) and 2^63 - 1.
#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
struct Long(i64);
#[Scalar]
impl ScalarType for Long {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_i64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value {
        Value::Number(self.0.into())
    }
}
#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
struct Byte(u8);
#[Scalar]
impl ScalarType for Byte {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        let Some(v) = number.as_u64() else {
            return Err(InputValueError::expected_type(value));
        };

        if let Ok(v) = u8::try_from(v) {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value {
        Value::Number(self.0.into())
    }
}

#[derive(serde::Serialize, serde::Deserialize)]
#[repr(transparent)]
#[serde(transparent)]
struct Decimal(f64);
#[Scalar]
impl ScalarType for Decimal {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_f64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value {
        let number = Number::from_f64(self.0).unwrap();
        Value::Number(number)
    }
}

/// The `TimeSpan` scalar represents an ISO-8601 compliant duration type.
#[derive(serde::Serialize, serde::Deserialize)]
#[repr(transparent)]
#[serde(transparent)]
struct TimeSpan(String);
#[Scalar]
impl ScalarType for TimeSpan {
    fn parse(value: Value) -> InputValueResult<Self> {
        todo!()
    }

    fn to_value(&self) -> Value {
        todo!()
    }
}

type BlockHeight = i64;
type BlockHash = String;
type TransactionHash = String;
type BakerId = i64;
type AccountIndex = i64;
type TransactionIndex = i64;
type Amount = i64; // TODO: should be UnsignedLong in graphQL
type Energy = i64; // TODO: should be UnsignedLong in graphQL
type DateTime = chrono::NaiveDateTime; // TODO check format matches.
type ContractIndex = UnsignedLong; // TODO check format.
type BigInteger = u64; // TODO check format.

#[derive(SimpleObject)]
struct Versions {
    backend_versions: String,
}

#[derive(SimpleObject, sqlx::FromRow)]
#[graphql(complex)]
struct Block {
    #[graphql(name = "blockHash")]
    hash: BlockHash,
    #[graphql(name = "blockHeight")]
    height: BlockHeight,
    /// Time of the block being baked.
    #[graphql(name = "blockSlotTime")]
    slot_time: DateTime,
    baker_id: Option<BakerId>,
    finalized: bool,
    //    chain_parameters: ChainParameters,
    // balance_statistics: BalanceStatistics,
    // block_statistics: BlockStatistics,
}

#[ComplexObject]
impl Block {
    /// Absolute block height.
    async fn id(&self) -> types::ID {
        types::ID::from(self.height)
    }

    /// Number of transactions included in this block.
    async fn transaction_count<'a>(&self, ctx: &Context<'a>) -> ApiResult<i64> {
        let result = sqlx::query!(
            "SELECT COUNT(*) FROM transactions WHERE block=$1",
            self.height
        )
        .fetch_one(get_pool(ctx)?)
        .await?;
        Ok(result.count.unwrap_or(0))
    }

    async fn special_events(
        &self,
        #[graphql(
            desc = "Filter special events by special event type. Set to null to return all special events (no filtering)."
        )]
        include_filters: Option<Vec<SpecialEventTypeFilter>>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, SpecialEvent>> {
        todo!()
    }
    async fn transactions(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        todo!()
    }
}

#[derive(Enum, Copy, Clone, PartialEq, Eq)]
enum SpecialEventTypeFilter {
    Mint,
    FinalizationRewards,
    BlockRewards,
    BakingRewards,
    PaydayAccountReward,
    BlockAccrueReward,
    PaydayFoundationReward,
    PaydayPoolReward,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct Contract {
    contract_address_index: ContractIndex,
    contract_address_sub_index: ContractIndex,
    contract_address: String,
    creator: AccountAddress,
    block_height: BlockHeight,
    transaction_hash: String,
    block_slot_time: DateTime,
    snapshot: ContractSnapshot,
}
#[ComplexObject]
impl Contract {
    async fn contract_events(&self, skip: i32, take: i32) -> ContractEventsCollectionSegment {
        todo!()
    }
    async fn contract_reject_events(
        &self,
        _skip: i32,
        _take: i32,
    ) -> ContractRejectEventsCollectionSegment {
        todo!()
    }
    async fn tokens(&self, skip: i32, take: i32) -> TokensCollectionSegment {
        todo!()
    }
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct TokensCollectionSegment {
    /// Information to aid in pagination.
    page_info: CollectionSegmentInfo,
    /// A flattened list of the items.
    items: Vec<Token>,
    total_count: i32,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ContractRejectEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info: CollectionSegmentInfo,
    /// A flattened list of the items.
    items: Vec<ContractRejectEvent>,
    total_count: i32,
}

#[derive(SimpleObject)]
struct ContractRejectEvent {
    contract_address_index: ContractIndex,
    contract_address_sub_index: ContractIndex,
    sender: AccountAddress,
    rejected_event: TransactionRejectReason,
    block_height: BlockHeight,
    transaction_hash: TransactionHash,
    block_slot_time: DateTime,
}

// union TransactionRejectReason = ModuleNotWf | ModuleHashAlreadyExists | InvalidAccountReference |
// InvalidInitMethod | InvalidReceiveMethod | InvalidModuleReference | InvalidContractAddress |
// RuntimeFailure | AmountTooLarge | SerializationFailure | OutOfEnergy | RejectedInit |
// RejectedReceive | NonExistentRewardAccount | InvalidProof | AlreadyABaker | NotABaker |
// InsufficientBalanceForBakerStake | StakeUnderMinimumThresholdForBaking | BakerInCooldown |
// DuplicateAggregationKey | NonExistentCredentialId | KeyIndexAlreadyInUse |
// InvalidAccountThreshold | InvalidCredentialKeySignThreshold | InvalidEncryptedAmountTransferProof
// | InvalidTransferToPublicProof | EncryptedAmountSelfTransfer | InvalidIndexOnEncryptedTransfer |
// ZeroScheduledAmount | NonIncreasingSchedule | FirstScheduledReleaseExpired |
// ScheduledSelfTransfer | InvalidCredentials | DuplicateCredIds | NonExistentCredIds |
// RemoveFirstCredential | CredentialHolderDidNotSign | NotAllowedMultipleCredentials |
// NotAllowedToReceiveEncrypted | NotAllowedToHandleEncrypted | MissingBakerAddParameters |
// FinalizationRewardCommissionNotInRange | BakingRewardCommissionNotInRange |
// TransactionFeeCommissionNotInRange | AlreadyADelegator | InsufficientBalanceForDelegationStake |
// MissingDelegationAddParameters | InsufficientDelegationStake | DelegatorInCooldown |
// NotADelegator | DelegationTargetNotABaker | StakeOverMaximumThresholdForPool |
// PoolWouldBecomeOverDelegated | PoolClosed
#[derive(Union, serde::Serialize, serde::Deserialize)]
enum TransactionRejectReason {
    PoolClosed(PoolClosed),
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct PoolClosed {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject)]
struct ContractSnapshot {
    block_height: BlockHeight,
    contract_address_index: ContractIndex,
    contract_address_sub_index: ContractIndex,
    contract_name: String,
    module_reference: String,
    amount: Amount,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ContractEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info: CollectionSegmentInfo,
    /// A flattened list of the items.
    items: Option<Vec<ContractEvent>>,
    total_count: i32,
}

#[derive(SimpleObject)]
struct ContractEvent {
    contract_address_index: ContractIndex,
    contract_address_sub_index: ContractIndex,
    sender: AccountAddress,
    event: Event,
    block_height: BlockHeight,
    transaction_hash: String,
    block_slot_time: DateTime,
}

/// Information about the offset pagination.
#[derive(SimpleObject)]
struct CollectionSegmentInfo {
    /// Indicates whether more items exist following the set defined by the clients arguments.
    has_next_page: bool,
    /// Indicates whether more items exist prior the set defined by the clients arguments.
    has_previous_page: bool,
}

#[derive(SimpleObject)]
struct AccountReward {
    block: Block,
    id: types::ID,
    timestamp: DateTime,
    reward_type: RewardType,
    amount: Amount,
}

#[derive(Enum, Copy, Clone, PartialEq, Eq)]
enum RewardType {
    FinalizationReward,
    FoundationReward,
    BakerReward,
    TransactionFeeReward,
}

#[derive(SimpleObject)]
struct AccountStatementEntry {
    reference: BlockOrTransaction,
    id: types::ID,
    timestamp: DateTime,
    entry_type: AccountStatementEntryType,
    amount: i64,
    account_balance: Amount,
}

#[derive(SimpleObject)]
struct AccountTransactionRelation {
    transaction: Transaction,
}

#[derive(SimpleObject)]
struct AccountAddressAmount {
    account_address: AccountAddress,
    amount: Amount,
}

#[derive(SimpleObject)]
struct AccountReleaseScheduleItem {
    transaction: Transaction,
    timestamp: DateTime,
    amount: Amount,
}

#[derive(SimpleObject)]
struct AccountToken {
    contract_index: ContractIndex,
    contract_sub_index: ContractIndex,
    token_id: String,
    balance: BigInteger,
    token: Token,
    account_id: i64,
    account: Account,
}

#[derive(SimpleObject)]
struct Token {
    initial_transaction: Transaction,
    contract_index: ContractIndex,
    contract_sub_index: ContractIndex,
    token_id: String,
    metadata_url: String,
    total_supply: BigInteger,
    contract_address_formatted: String,
    token_address: String,
    // TODO accounts(skip: Int take: Int): AccountsCollectionSegment
    // TODO tokenEvents(skip: Int take: Int): TokenEventsCollectionSegment
}

#[derive(Union)]
enum SpecialEvent {
    MintSpecialEvent(MintSpecialEvent),
    FinalizationRewardsSpecialEvent(FinalizationRewardsSpecialEvent),
    BlockRewardsSpecialEvent(BlockRewardsSpecialEvent),
    BakingRewardsSpecialEvent(BakingRewardsSpecialEvent),
    PaydayAccountRewardSpecialEvent(PaydayAccountRewardSpecialEvent),
    BlockAccrueRewardSpecialEvent(BlockAccrueRewardSpecialEvent),
    PaydayFoundationRewardSpecialEvent(PaydayFoundationRewardSpecialEvent),
    PaydayPoolRewardSpecialEvent(PaydayPoolRewardSpecialEvent),
}

#[derive(SimpleObject)]
struct MintSpecialEvent {
    baking_reward: Amount,
    finalization_reward: Amount,
    platform_development_charge: Amount,
    foundation_account_address: AccountAddress,
    id: types::ID,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct FinalizationRewardsSpecialEvent {
    remainder: Amount,
    id: types::ID,
}

#[ComplexObject]
impl FinalizationRewardsSpecialEvent {
    async fn finalization_rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountAddressAmount>> {
        todo!()
    }
}

#[derive(SimpleObject)]
struct BlockRewardsSpecialEvent {
    transaction_fees: Amount,
    old_gas_account: Amount,
    new_gas_account: Amount,
    baker_reward: Amount,
    foundation_charge: Amount,
    baker_account_address: AccountAddress,
    foundation_account_address: AccountAddress,
    id: types::ID,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct BakingRewardsSpecialEvent {
    remainder: Amount,
    id: types::ID,
}
#[ComplexObject]
impl BakingRewardsSpecialEvent {
    async fn baking_rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountAddressAmount>> {
        todo!()
    }
}

#[derive(SimpleObject)]
struct PaydayAccountRewardSpecialEvent {
    /// The account that got rewarded.
    account: AccountAddress,
    /// The transaction fee reward at payday to the account.
    transaction_fees: Amount,
    /// The baking reward at payday to the account.
    baker_reward: Amount,
    /// The finalization reward at payday to the account.
    finalization_reward: Amount,
    id: types::ID,
}

#[derive(SimpleObject)]
struct BlockAccrueRewardSpecialEvent {
    /// The total fees paid for transactions in the block.
    transaction_fees: Amount,
    /// The old balance of the GAS account.
    old_gas_account: Amount,
    /// The new balance of the GAS account.
    new_gas_account: Amount,
    /// The amount awarded to the baker.
    baker_reward: Amount,
    /// The amount awarded to the passive delegators.
    passive_reward: Amount,
    /// The amount awarded to the foundation.
    foundation_charge: Amount,
    /// The baker of the block, who will receive the award.
    baker_id: BakerId,
    id: types::ID,
}

#[derive(SimpleObject)]
struct PaydayFoundationRewardSpecialEvent {
    foundation_account: AccountAddress,
    development_charge: Amount,
    id: types::ID,
}

#[derive(SimpleObject)]
struct PaydayPoolRewardSpecialEvent {
    /// The pool awarded.
    pool: PoolRewardTarget,
    /// Accrued transaction fees for pool.
    transaction_fees: Amount,
    /// Accrued baking rewards for pool.
    baker_reward: Amount,
    /// Accrued finalization rewards for pool.
    finalization_reward: Amount,
    id: types::ID,
}

#[derive(Union)]
enum PoolRewardTarget {
    PassiveDelegationPoolRewardTarget(PassiveDelegationPoolRewardTarget),
    BakerPoolRewardTarget(BakerPoolRewardTarget),
}

#[derive(SimpleObject)]
struct PassiveDelegationPoolRewardTarget {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct PassiveDelegationTarget {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerPoolRewardTarget {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerDelegationTarget {
    baker_id: BakerId,
}

#[derive(SimpleObject)]
struct BalanceStatistics {
    /// The total CCD in existence
    total_amount: Amount,
    /// The total CCD Released. This is total CCD supply not counting the balances of non
    /// circulating accounts.
    total_amount_released: Amount,
    /// The total CCD Unlocked according to the Concordium promise published on
    /// deck.concordium.com. Will be null for blocks with slot time before the published release
    /// schedule.
    total_amount_unlocked: Amount,
    /// The total CCD in encrypted balances.
    total_amount_encrypted: Amount,
    /// The total CCD locked in release schedules (from transfers with schedule).
    total_amount_locked_in_release_schedules: Amount,
    /// The total CCD staked.
    total_amount_staked: Amount,
    /// The amount in the baking reward account.
    baking_reward_account: Amount,
    /// The amount in the finalization reward account.
    finalization_reward_account: Amount,
    /// The amount in the GAS account.
    gas_account: Amount,
}

#[derive(SimpleObject)]
struct BlockStatistics {
    block_time: f32,
    finalization_time: f32,
}

#[derive(Interface)]
#[graphql(
    field(name = "euro_per_energy", ty = "&ExchangeRate"),
    field(name = "micro_ccd_per_euro", ty = "&ExchangeRate"),
    field(name = "account_creation_limit", ty = "&i32"),
    field(name = "foundation_account_address", ty = "&AccountAddress")
)]
enum ChainParameters {
    ChainParametersV0(ChainParametersV0),
    ChainParametersV1(ChainParametersV1),
    ChainParametersV2(ChainParametersV2),
}

#[derive(SimpleObject)]
struct ChainParametersV0 {
    // TODO
    //   electionDifficulty: Decimal!
    // bakerCooldownEpochs: UnsignedLong!
    // rewardParameters: RewardParametersV0!
    // minimumThresholdForBaking: UnsignedLong!
    euro_per_energy: ExchangeRate,
    micro_ccd_per_euro: ExchangeRate,
    account_creation_limit: i32,
    foundation_account_address: AccountAddress,
}

#[derive(SimpleObject)]
struct ChainParametersV1 {
    // TODO
    // electionDifficulty: Decimal!
    //     poolOwnerCooldown: UnsignedLong!
    //     delegatorCooldown: UnsignedLong!
    //     rewardPeriodLength: UnsignedLong!
    //     mintPerPayday: Decimal!
    //     rewardParameters: RewardParametersV1!
    //     passiveFinalizationCommission: Decimal!
    //     passiveBakingCommission: Decimal!
    //     passiveTransactionCommission: Decimal!
    //     finalizationCommissionRange: CommissionRange!
    //     bakingCommissionRange: CommissionRange!
    //     transactionCommissionRange: CommissionRange!
    //     minimumEquityCapital: UnsignedLong!
    //     capitalBound: Decimal!
    //     leverageBound: LeverageFactor!
    euro_per_energy: ExchangeRate,
    micro_ccd_per_euro: ExchangeRate,
    account_creation_limit: i32,
    foundation_account_address: AccountAddress,
}

#[derive(SimpleObject)]
struct ChainParametersV2 {
    // TODO
    // poolOwnerCooldown: UnsignedLong!
    // delegatorCooldown: UnsignedLong!
    // rewardPeriodLength: UnsignedLong!
    // mintPerPayday: Decimal!
    // rewardParameters: RewardParametersV2!
    // passiveFinalizationCommission: Decimal!
    // passiveBakingCommission: Decimal!
    // passiveTransactionCommission: Decimal!
    // finalizationCommissionRange: CommissionRange!
    // bakingCommissionRange: CommissionRange!
    // transactionCommissionRange: CommissionRange!
    // minimumEquityCapital: UnsignedLong!
    // capitalBound: Decimal!
    // leverageBound: LeverageFactor!
    euro_per_energy: ExchangeRate,
    micro_ccd_per_euro: ExchangeRate,
    account_creation_limit: i32,
    foundation_account_address: AccountAddress,
}

#[derive(SimpleObject)]
struct ExchangeRate {
    numerator: u64,
    denominator: u64,
}

#[derive(SimpleObject, Clone, serde::Serialize, serde::Deserialize)]
struct AccountAddress {
    as_string: String,
}

impl From<concordium_rust_sdk::common::types::AccountAddress> for AccountAddress {
    fn from(address: concordium_rust_sdk::common::types::AccountAddress) -> Self {
        address.to_string().into()
    }
}

impl From<String> for AccountAddress {
    fn from(as_string: String) -> Self {
        Self { as_string }
    }
}

struct IdTransaction {
    block: BlockHeight,
    index: TransactionIndex,
}

impl TryFrom<types::ID> for IdTransaction {
    type Error = ApiError;
    fn try_from(value: types::ID) -> Result<Self, Self::Error> {
        let (height_str, index_str) = value
            .as_str()
            .split_once(':')
            .ok_or(ApiError::InvalidIdTransaction)?;
        Ok(IdTransaction {
            block: height_str.parse().map_err(ApiError::InvalidIdInt)?,
            index: index_str.parse().map_err(ApiError::InvalidIdInt)?,
        })
    }
}
impl From<IdTransaction> for types::ID {
    fn from(value: IdTransaction) -> Self {
        types::ID::from(format!("{}:{}", value.block, value.index))
    }
}

#[derive(SimpleObject, sqlx::FromRow)]
#[graphql(complex)]
struct Transaction {
    #[graphql(skip)]
    block: BlockHeight,
    #[graphql(name = "transactionIndex")]
    index: i64,
    #[graphql(name = "transactionHash")]
    hash: TransactionHash,
    ccd_cost: Amount,
    energy_cost: Energy,
    #[graphql(skip)]
    sender: Option<AccountIndex>,
    #[graphql(skip)]
    r#type: DbTransactionType,
    #[graphql(skip)]
    type_account: Option<AccountTransactionType>,
    #[graphql(skip)]
    type_credential_deployment: Option<CredentialDeploymentTransactionType>,
    #[graphql(skip)]
    type_update: Option<UpdateTransactionType>,
    #[graphql(skip)]
    success: bool,
    #[graphql(skip)]
    #[sqlx(json)]
    events: Option<Vec<Event>>,
    #[graphql(skip)]
    #[sqlx(json)]
    reject: Option<TransactionRejectReason>,
}
#[ComplexObject]
impl Transaction {
    /// Transaction query ID, formatted as "<block>:<index>".
    async fn id(&self) -> types::ID {
        IdTransaction {
            block: self.block,
            index: self.index,
        }
        .into()
    }

    async fn block<'a>(&self, ctx: &Context<'a>) -> ApiResult<Block> {
        let result = sqlx::query_as!(Block, "SELECT * FROM blocks WHERE height=$1", self.block)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(result)
    }

    async fn sender_account_address<'a>(
        &self,
        ctx: &Context<'a>,
    ) -> ApiResult<Option<AccountAddress>> {
        let Some(account_index) = self.sender else {
            return Ok(None);
        };
        let result = sqlx::query!("SELECT address FROM accounts WHERE index=$1", account_index)
            .fetch_one(get_pool(ctx)?)
            .await?;
        Ok(Some(result.address.into()))
    }

    async fn transaction_type(&self) -> ApiResult<TransactionType> {
        let tt = match self.r#type {
            DbTransactionType::Account => TransactionType::AccountTransaction(AccountTransaction {
                account_transaction_type: self.type_account,
            }),
            DbTransactionType::CredentialDeployment => TransactionType::CredentialDeploymentTransaction(CredentialDeploymentTransaction {
                credential_deployment_transaction_type: self.type_credential_deployment.ok_or(ApiError::InternalError("Database invariant violated, transaction type is credential deployment, but credential deployment type is null".to_string()))?,
            }),
            DbTransactionType::Update => TransactionType::UpdateTransaction(UpdateTransaction {
                update_transaction_type: self.type_update.ok_or(ApiError::InternalError("Database invariant violated, transaction type is update, but update type is null".to_string()))?,
            }),
        };
        Ok(tt)
    }

    async fn result(&self) -> ApiResult<TransactionResult<'_>> {
        if self.success {
            let events = self.events.as_ref().ok_or(ApiError::InternalError(
                "Success events is null".to_string(),
            ))?;
            Ok(TransactionResult::Success(Success { events }))
        } else {
            let reason = self.reject.as_ref().ok_or(ApiError::InternalError(
                "Success events is null".to_string(),
            ))?;
            Ok(TransactionResult::Rejected(Rejected { reason }))
        }
    }
}

#[derive(Union)]
enum TransactionType {
    AccountTransaction(AccountTransaction),
    CredentialDeploymentTransaction(CredentialDeploymentTransaction),
    UpdateTransaction(UpdateTransaction),
}

#[derive(SimpleObject)]
struct AccountTransaction {
    account_transaction_type: Option<AccountTransactionType>,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "account_transaction_type")]
pub enum AccountTransactionType {
    InitializeSmartContractInstance,
    UpdateSmartContractInstance,
    SimpleTransfer,
    EncryptedTransfer,
    SimpleTransferWithMemo,
    EncryptedTransferWithMemo,
    TransferWithScheduleWithMemo,
    DeployModule,
    AddBaker,
    RemoveBaker,
    UpdateBakerStake,
    UpdateBakerRestakeEarnings,
    UpdateBakerKeys,
    UpdateCredentialKeys,
    TransferToEncrypted,
    TransferToPublic,
    TransferWithSchedule,
    UpdateCredentials,
    RegisterData,
    ConfigureBaker,
    ConfigureDelegation,
}

impl From<concordium_rust_sdk::types::TransactionType> for AccountTransactionType {
    fn from(value: concordium_rust_sdk::types::TransactionType) -> Self {
        use concordium_rust_sdk::types::TransactionType as TT;
        use AccountTransactionType as ATT;
        match value {
            TT::DeployModule => ATT::DeployModule,
            TT::InitContract => ATT::InitializeSmartContractInstance,
            TT::Update => ATT::UpdateSmartContractInstance,
            TT::Transfer => ATT::SimpleTransfer,
            TT::AddBaker => ATT::AddBaker,
            TT::RemoveBaker => ATT::RemoveBaker,
            TT::UpdateBakerStake => ATT::UpdateBakerStake,
            TT::UpdateBakerRestakeEarnings => ATT::UpdateBakerRestakeEarnings,
            TT::UpdateBakerKeys => ATT::UpdateBakerKeys,
            TT::UpdateCredentialKeys => ATT::UpdateCredentialKeys,
            TT::EncryptedAmountTransfer => ATT::EncryptedTransfer,
            TT::TransferToEncrypted => ATT::TransferToEncrypted,
            TT::TransferToPublic => ATT::TransferToPublic,
            TT::TransferWithSchedule => ATT::TransferWithSchedule,
            TT::UpdateCredentials => ATT::UpdateCredentials,
            TT::RegisterData => ATT::RegisterData,
            TT::TransferWithMemo => ATT::SimpleTransferWithMemo,
            TT::EncryptedAmountTransferWithMemo => ATT::EncryptedTransferWithMemo,
            TT::TransferWithScheduleAndMemo => ATT::TransferWithScheduleWithMemo,
            TT::ConfigureBaker => ATT::ConfigureBaker,
            TT::ConfigureDelegation => ATT::ConfigureDelegation,
        }
    }
}

#[derive(SimpleObject)]
struct CredentialDeploymentTransaction {
    credential_deployment_transaction_type: CredentialDeploymentTransactionType,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "credential_deployment_transaction_type")]
pub enum CredentialDeploymentTransactionType {
    Initial,
    Normal,
}

impl From<concordium_rust_sdk::types::CredentialType> for CredentialDeploymentTransactionType {
    fn from(value: concordium_rust_sdk::types::CredentialType) -> Self {
        use concordium_rust_sdk::types::CredentialType;
        match value {
            CredentialType::Initial => CredentialDeploymentTransactionType::Initial,
            CredentialType::Normal => CredentialDeploymentTransactionType::Normal,
        }
    }
}

#[derive(SimpleObject)]
struct UpdateTransaction {
    update_transaction_type: UpdateTransactionType,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "update_transaction_type")]
pub enum UpdateTransactionType {
    UpdateProtocol,
    UpdateElectionDifficulty,
    UpdateEuroPerEnergy,
    UpdateMicroGtuPerEuro,
    UpdateFoundationAccount,
    UpdateMintDistribution,
    UpdateTransactionFeeDistribution,
    UpdateGasRewards,
    UpdateBakerStakeThreshold,
    UpdateAddAnonymityRevoker,
    UpdateAddIdentityProvider,
    UpdateRootKeys,
    UpdateLevel1Keys,
    UpdateLevel2Keys,
    UpdatePoolParameters,
    UpdateCooldownParameters,
    UpdateTimeParameters,
    MintDistributionCpv1Update,
    GasRewardsCpv2Update,
    TimeoutParametersUpdate,
    MinBlockTimeUpdate,
    BlockEnergyLimitUpdate,
    FinalizationCommitteeParametersUpdate,
}

impl From<concordium_rust_sdk::types::UpdateType> for UpdateTransactionType {
    fn from(value: concordium_rust_sdk::types::UpdateType) -> Self {
        use concordium_rust_sdk::types::UpdateType;
        match value {
            UpdateType::UpdateProtocol => UpdateTransactionType::UpdateProtocol,
            UpdateType::UpdateElectionDifficulty => UpdateTransactionType::UpdateElectionDifficulty,
            UpdateType::UpdateEuroPerEnergy => UpdateTransactionType::UpdateEuroPerEnergy,
            UpdateType::UpdateMicroGTUPerEuro => UpdateTransactionType::UpdateMicroGtuPerEuro,
            UpdateType::UpdateFoundationAccount => UpdateTransactionType::UpdateFoundationAccount,
            UpdateType::UpdateMintDistribution => UpdateTransactionType::UpdateMintDistribution,
            UpdateType::UpdateTransactionFeeDistribution => {
                UpdateTransactionType::UpdateTransactionFeeDistribution
            },
            UpdateType::UpdateGASRewards => UpdateTransactionType::UpdateGasRewards,
            UpdateType::UpdateAddAnonymityRevoker => {
                UpdateTransactionType::UpdateAddAnonymityRevoker
            },
            UpdateType::UpdateAddIdentityProvider => {
                UpdateTransactionType::UpdateAddIdentityProvider
            },
            UpdateType::UpdateRootKeys => UpdateTransactionType::UpdateRootKeys,
            UpdateType::UpdateLevel1Keys => UpdateTransactionType::UpdateLevel1Keys,
            UpdateType::UpdateLevel2Keys => UpdateTransactionType::UpdateLevel2Keys,
            UpdateType::UpdatePoolParameters => UpdateTransactionType::UpdatePoolParameters,
            UpdateType::UpdateCooldownParameters => UpdateTransactionType::UpdateCooldownParameters,
            UpdateType::UpdateTimeParameters => UpdateTransactionType::UpdateTimeParameters,
            UpdateType::UpdateGASRewardsCPV2 => UpdateTransactionType::GasRewardsCpv2Update,
            UpdateType::UpdateTimeoutParameters => UpdateTransactionType::TimeoutParametersUpdate,
            UpdateType::UpdateMinBlockTime => UpdateTransactionType::MinBlockTimeUpdate,
            UpdateType::UpdateBlockEnergyLimit => UpdateTransactionType::BlockEnergyLimitUpdate,
            UpdateType::UpdateFinalizationCommitteeParameters => {
                UpdateTransactionType::FinalizationCommitteeParametersUpdate
            },
        }
    }
}

#[derive(Union)]
enum TransactionResult<'a> {
    Success(Success<'a>),
    Rejected(Rejected<'a>),
}

struct Success<'a> {
    events: &'a Vec<Event>,
}

#[Object]
impl Success<'_> {
    async fn events(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i64,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i64,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, Event>> {
        todo!()
    }
}

#[derive(SimpleObject)]
struct Rejected<'a> {
    reason: &'a TransactionRejectReason,
}

#[derive(SimpleObject, sqlx::FromRow)]
#[graphql(complex)]
struct Account {
    // release_schedule: AccountReleaseSchedule,
    #[graphql(skip)]
    index: i64,
    /// Height of the block with the transaction creating this account.
    #[graphql(skip)]
    created_block: BlockHeight,
    /// Index of transaction creating this account within a block. Only Null for genesis accounts.
    #[graphql(skip)]
    created_index: Option<TransactionIndex>,
    /// The address of the account in Base58Check.
    #[sqlx(try_from = "String")]
    address: AccountAddress,
    /// The total amount of CCD hold by the account.
    amount: Amount,
    // Get baker information if this account is baking.
    // baker: Option<Baker>,
    // delegation: Option<Delegation>,
}

#[ComplexObject]
impl Account {
    async fn id(&self) -> types::ID {
        types::ID::from(self.index)
    }

    /// Timestamp of the block where this account was created.
    async fn created_at<'a>(&self, ctx: &Context<'a>) -> ApiResult<DateTime> {
        let rec = sqlx::query!(
            "SELECT slot_time FROM blocks WHERE height=$1",
            self.created_block
        )
        .fetch_one(get_pool(ctx)?)
        .await?;
        Ok(rec.slot_time)
    }

    /// Number of transactions where this account is used as sender.
    async fn transaction_count<'a>(&self, ctx: &Context<'a>) -> ApiResult<i64> {
        let rec = sqlx::query!(
            "SELECT COUNT(*) FROM transactions WHERE sender=$1",
            self.index
        )
        .fetch_one(get_pool(ctx)?)
        .await?;
        Ok(rec.count.unwrap_or(0))
    }

    async fn tokens(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountToken>> {
        todo!()
    }
    async fn transactions(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountTransactionRelation>> {
        todo!()
    }
    async fn account_statement(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountStatementEntry>> {
        todo!()
    }
    async fn rewards(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountReward>> {
        todo!()
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct AccountReleaseSchedule {
    total_amount: Amount,
}
#[ComplexObject]
impl AccountReleaseSchedule {
    async fn schedule(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: i32,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: String,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: i32,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        before: String,
    ) -> ApiResult<connection::Connection<String, AccountReleaseScheduleItem>> {
        todo!()
    }
}

#[derive(SimpleObject)]
struct Baker {
    account: Box<Account>,
    id: types::ID,
    baker_id: BakerId,
    state: BakerState,
    //      /// Get the transactions that have affected the baker.
    // transactions("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): BakerTransactionRelationConnection
}

#[derive(Union)]
enum BakerState {
    ActiveBakerState(ActiveBakerState),
    RemovedBakerState(RemovedBakerState),
}

#[derive(SimpleObject)]
struct ActiveBakerState {
    /// The status of the bakers node. Will be null if no status for the node exists.
    node_status: NodeStatus,
    staked_amount: Amount,
    restake_earnings: bool,
    pool: BakerPool,
    pending_change: PendingBakerChange,
}

#[derive(Union)]
enum PendingBakerChange {
    PendingBakerRemoval(PendingBakerRemoval),
    PendingBakerReduceStake(PendingBakerReduceStake),
}

#[derive(SimpleObject)]
struct PendingBakerRemoval {
    effective_time: DateTime,
}

#[derive(SimpleObject)]
struct PendingBakerReduceStake {
    new_staked_amount: Amount,
    effective_time: DateTime,
}

#[derive(SimpleObject)]
struct RemovedBakerState {
    removed_at: DateTime,
}

#[derive(SimpleObject)]
struct NodeStatus {
    // TODO: add below fields
    // peersList: [PeerReference!]!
    // nodeName: String
    // nodeId: String!
    // peerType: String!
    // uptime: UnsignedLong!
    // clientVersion: String!
    // averagePing: Float
    // peersCount: UnsignedLong!
    // bestBlock: String!
    // bestBlockHeight: UnsignedLong!
    // bestBlockBakerId: UnsignedLong
    // bestArrivedTime: DateTime
    // blockArrivePeriodEma: Float
    // blockArrivePeriodEmsd: Float
    // blockArriveLatencyEma: Float
    // blockArriveLatencyEmsd: Float
    // blockReceivePeriodEma: Float
    // blockReceivePeriodEmsd: Float
    // blockReceiveLatencyEma: Float
    // blockReceiveLatencyEmsd: Float
    // finalizedBlock: String!
    // finalizedBlockHeight: UnsignedLong!
    // finalizedTime: DateTime
    // finalizationPeriodEma: Float
    // finalizationPeriodEmsd: Float
    // packetsSent: UnsignedLong!
    // packetsReceived: UnsignedLong!
    // consensusRunning: Boolean!
    // bakingCommitteeMember: String!
    // consensusBakerId: UnsignedLong
    // finalizationCommitteeMember: Boolean!
    // transactionsPerBlockEma: Float
    // transactionsPerBlockEmsd: Float
    // bestBlockTransactionsSize: UnsignedLong
    // bestBlockTotalEncryptedAmount: UnsignedLong
    // bestBlockTotalAmount: UnsignedLong
    // bestBlockTransactionCount: UnsignedLong
    // bestBlockTransactionEnergyCost: UnsignedLong
    // bestBlockExecutionCost: UnsignedLong
    // bestBlockCentralBankAmount: UnsignedLong
    // blocksReceivedCount: UnsignedLong
    // blocksVerifiedCount: UnsignedLong
    // genesisBlock: String!
    // finalizationCount: UnsignedLong
    // finalizedBlockParent: String!
    // averageBytesPerSecondIn: Float!
    // averageBytesPerSecondOut: Float!
    id: types::ID,
}

#[derive(SimpleObject)]
struct BakerPool {
    /// Total stake of the baker pool as a percentage of all CCDs in existence. Value may be null
    /// for brand new bakers where statistics have not been calculated yet. This should be rare and
    /// only a temporary condition.
    total_stake_percentage: Decimal,
    lottery_power: Decimal,
    payday_commission_rates: CommissionRates,
    open_status: BakerPoolOpenStatus,
    commission_rates: CommissionRates,
    metadata_url: String,
    /// The total amount staked by delegation to this baker pool.
    delegated_stake: Amount,
    /// The maximum amount that may be delegated to the pool, accounting for leverage and stake
    /// limits.
    delegated_stake_cap: Amount,
    /// The total amount staked in this baker pool. Includes both baker stake and delegated stake.
    total_stake: Amount,
    delegator_count: i32,
    /// Ranking of the baker pool by total staked amount. Value may be null for brand new bakers
    /// where statistics have not been calculated yet. This should be rare and only a temporary
    /// condition.
    ranking_by_total_stake: Ranking,
    // TODO: apy(period: ApyPeriod!): PoolApy!
    // TODO: delegators("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): DelegatorsConnection
    // TODO: poolRewards("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): PaydayPoolRewardConnection
}

#[derive(SimpleObject)]
struct CommissionRates {
    transaction_commission: Decimal,
    finalization_commission: Decimal,
    baking_commission: Decimal,
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
enum BakerPoolOpenStatus {
    OpenForAll,
    ClosedForNew,
    ClosedForAll,
}

impl From<concordium_rust_sdk::types::OpenStatus> for BakerPoolOpenStatus {
    fn from(status: concordium_rust_sdk::types::OpenStatus) -> Self {
        use concordium_rust_sdk::types::OpenStatus;
        match status {
            OpenStatus::OpenForAll => Self::OpenForAll,
            OpenStatus::ClosedForNew => Self::ClosedForNew,
            OpenStatus::ClosedForAll => Self::ClosedForAll,
        }
    }
}

#[derive(SimpleObject)]
struct Ranking {
    rank: i32,
    total: i32,
}

#[derive(SimpleObject)]
struct Delegation {
    delegator_id: i64,
    staked_amount: Amount,
    restake_earnings: bool,
    delegation_target: DelegationTarget,
    pending_change: PendingDelegationChange,
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
enum DelegationTarget {
    PassiveDelegationTarget(PassiveDelegationTarget),
    BakerDelegationTarget(BakerDelegationTarget),
}

impl TryFrom<concordium_rust_sdk::types::DelegationTarget> for DelegationTarget {
    type Error = anyhow::Error;
    fn try_from(target: concordium_rust_sdk::types::DelegationTarget) -> Result<Self, Self::Error> {
        use concordium_rust_sdk::types::DelegationTarget as Target;
        match target {
            Target::Passive => {
                Ok(DelegationTarget::PassiveDelegationTarget(
                    PassiveDelegationTarget { dummy: true },
                ))
            },
            Target::Baker { baker_id } => {
                Ok(DelegationTarget::BakerDelegationTarget(
                    BakerDelegationTarget {
                        baker_id: baker_id.id.index.try_into()?,
                    },
                ))
            },
        }
    }
}

#[derive(Union)]
enum PendingDelegationChange {
    PendingDelegationRemoval(PendingDelegationRemoval),
    PendingDelegationReduceStake(PendingDelegationReduceStake),
}

#[derive(SimpleObject)]
struct PendingDelegationRemoval {
    effective_time: DateTime,
}

#[derive(SimpleObject)]
struct PendingDelegationReduceStake {
    new_staked_amount: Amount,
    effective_time: DateTime,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum AccountStatementEntryType {
    TransferIn,
    TransferOut,
    AmountDecrypted,
    AmountEncrypted,
    TransactionFee,
    FinalizationReward,
    FoundationReward,
    BakerReward,
    TransactionFeeReward,
}

#[derive(Union)]
enum BlockOrTransaction {
    Transaction(Transaction),
    Block(Block),
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, Default)]
enum AccountSort {
    AgeAsc,
    #[default]
    AgeDesc,
    AmountAsc,
    AmountDesc,
    TransactionCountAsc,
    TransactionCountDesc,
    DelegatedStakeAsc,
    DelegatedStakeDesc,
}

#[derive(InputObject)]
struct AccountFilterInput {
    is_delegator: bool,
}

#[derive(InputObject)]
struct BakerFilterInput {
    open_status_filter: BakerPoolOpenStatus,
    include_removed: bool,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, Default)]
enum BakerSort {
    #[default]
    BakerIdAsc,
    BakerIdDesc,
    BakerStakedAmountAsc,
    BakerStakedAmountDesc,
    TotalStakedAmountAsc,
    TotalStakedAmountDesc,
    DelegatorCountAsc,
    DelegatorCountDesc,
    BakerApy30DaysDesc,
    DelegatorApy30DaysDesc,
    BlockCommissionsAsc,
    BlockCommissionsDesc,
}

struct SearchResult {
    _query: String,
}

#[Object]
impl SearchResult {
    async fn contracts<'a>(
        &self,
        _ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Contract>> {
        todo!()
    }

    // async fn modules(
    //     &self,
    //     #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
    //     #[graphql(desc = "Returns the elements in the list that come after the specified
    // cursor.")]     _after: Option<String>,
    //     #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
    //     #[graphql(
    //         desc = "Returns the elements in the list that come before the specified cursor."
    //     )]
    //     _before: Option<String>,
    // ) -> ApiResult<connection::Connection<String, Module>> {
    //     todo!()
    // }

    async fn blocks(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Block>> {
        todo!()
    }

    async fn transactions(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Transaction>> {
        todo!()
    }

    async fn tokens(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Token>> {
        todo!()
    }

    async fn accounts(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Account>> {
        todo!()
    }

    async fn bakers(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        todo!()
    }

    async fn node_statuses(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<i32>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<i32>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, NodeStatus>> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractAddress {
    index: ContractIndex,
    sub_index: ContractIndex,
    as_string: String,
}

impl From<concordium_rust_sdk::types::ContractAddress> for ContractAddress {
    fn from(value: concordium_rust_sdk::types::ContractAddress) -> Self {
        Self {
            index: value.index.into(),
            sub_index: value.subindex.into(),
            as_string: value.to_string(),
        }
    }
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
enum Address {
    ContractAddress(ContractAddress),
    AccountAddress(AccountAddress),
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum Event {
    Transferred(Transferred),
    AccountCreated(AccountCreated),
    AmountAddedByDecryption(AmountAddedByDecryption),
    BakerAdded(BakerAdded),
    BakerKeysUpdated(BakerKeysUpdated),
    BakerRemoved(BakerRemoved),
    BakerSetRestakeEarnings(BakerSetRestakeEarnings),
    BakerStakeDecreased(BakerStakeDecreased),
    BakerStakeIncreased(BakerStakeIncreased),
    ContractInitialized(ContractInitialized),
    ContractModuleDeployed(ContractModuleDeployed),
    ContractUpdated(ContractUpdated),
    ContractCall(ContractCall),
    CredentialDeployed(CredentialDeployed),
    CredentialKeysUpdated(CredentialKeysUpdated),
    CredentialsUpdated(CredentialsUpdated),
    DataRegistered(DataRegistered),
    EncryptedAmountsRemoved(EncryptedAmountsRemoved),
    EncryptedSelfAmountAdded(EncryptedSelfAmountAdded),
    NewEncryptedAmount(NewEncryptedAmount),
    TransferMemo(TransferMemo),
    TransferredWithSchedule(TransferredWithSchedule),
    ChainUpdateEnqueued(ChainUpdateEnqueued),
    ContractInterrupted(ContractInterrupted),
    ContractResumed(ContractResumed),
    ContractUpgraded(ContractUpgraded),
    BakerSetOpenStatus(BakerSetOpenStatus),
    BakerSetMetadataURL(BakerSetMetadataURL),
    BakerSetTransactionFeeCommission(BakerSetTransactionFeeCommission),
    BakerSetBakingRewardCommission(BakerSetBakingRewardCommission),
    BakerSetFinalizationRewardCommission(BakerSetFinalizationRewardCommission),
    DelegationAdded(DelegationAdded),
    DelegationRemoved(DelegationRemoved),
    DelegationStakeIncreased(DelegationStakeIncreased),
    DelegationStakeDecreased(DelegationStakeDecreased),
    DelegationSetRestakeEarnings(DelegationSetRestakeEarnings),
    DelegationSetDelegationTarget(DelegationSetDelegationTarget),
}

pub fn events_from_summary(
    value: concordium_rust_sdk::types::BlockItemSummaryDetails,
) -> anyhow::Result<Vec<Event>> {
    use concordium_rust_sdk::types::{
        AccountTransactionEffects,
        BlockItemSummaryDetails,
    };
    let events = match value {
        BlockItemSummaryDetails::AccountTransaction(details) => {
            match details.effects {
                AccountTransactionEffects::None {
                    transaction_type,
                    reject_reason,
                } => todo!(),
                AccountTransactionEffects::ModuleDeployed { module_ref } => {
                    vec![Event::ContractModuleDeployed(ContractModuleDeployed {
                        module_ref: module_ref.to_string(),
                    })]
                },
                AccountTransactionEffects::ContractInitialized { data } => {
                    vec![Event::ContractInitialized(ContractInitialized {
                        module_ref: data.origin_ref.to_string(),
                        contract_address: data.address.into(),
                        amount: i64::try_from(data.amount.micro_ccd)?,
                        init_name: data.init_name.to_string(),
                        version: data.contract_version.into(),
                    })]
                },
                AccountTransactionEffects::ContractUpdateIssued { effects } => todo!(),
                AccountTransactionEffects::AccountTransfer { amount, to } => {
                    vec![Event::Transferred(Transferred {
                        amount: i64::try_from(amount.micro_ccd)?,
                        from: details.sender.into(),
                        to: to.into(),
                    })]
                },
                AccountTransactionEffects::AccountTransferWithMemo { amount, to, memo } => {
                    vec![
                        Event::Transferred(Transferred {
                            amount: i64::try_from(amount.micro_ccd)?,
                            from: details.sender.into(),
                            to: to.into(),
                        }),
                        Event::TransferMemo(memo.into()),
                    ]
                },
                AccountTransactionEffects::BakerAdded { data } => {
                    vec![Event::BakerAdded(BakerAdded {
                        staked_amount: data.stake.micro_ccd.try_into()?,
                        restake_earnings: data.restake_earnings,
                        baker_id: data.keys_event.baker_id.id.index.try_into()?,
                        sign_key: serde_json::to_string(&data.keys_event.sign_key)?,
                        election_key: serde_json::to_string(&data.keys_event.election_key)?,
                        aggregation_key: serde_json::to_string(&data.keys_event.aggregation_key)?,
                    })]
                },
                AccountTransactionEffects::BakerRemoved { baker_id } => {
                    vec![Event::BakerRemoved(BakerRemoved {
                        baker_id: baker_id.id.index.try_into()?,
                    })]
                },
                AccountTransactionEffects::BakerStakeUpdated { data } => {
                    if let Some(data) = data {
                        if data.increased {
                            vec![Event::BakerStakeIncreased(BakerStakeIncreased {
                                baker_id: data.baker_id.id.index.try_into()?,
                                new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                            })]
                        } else {
                            vec![Event::BakerStakeDecreased(BakerStakeDecreased {
                                baker_id: data.baker_id.id.index.try_into()?,
                                new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                            })]
                        }
                    } else {
                        Vec::new()
                    }
                },
                AccountTransactionEffects::BakerRestakeEarningsUpdated {
                    baker_id,
                    restake_earnings,
                } => {
                    vec![Event::BakerSetRestakeEarnings(BakerSetRestakeEarnings {
                        baker_id: baker_id.id.index.try_into()?,
                        restake_earnings,
                    })]
                },
                AccountTransactionEffects::BakerKeysUpdated { data } => {
                    vec![Event::BakerKeysUpdated(BakerKeysUpdated {
                        baker_id: data.baker_id.id.index.try_into()?,
                        sign_key: serde_json::to_string(&data.sign_key)?,
                        election_key: serde_json::to_string(&data.election_key)?,
                        aggregation_key: serde_json::to_string(&data.aggregation_key)?,
                    })]
                },
                AccountTransactionEffects::EncryptedAmountTransferred { removed, added } => {
                    vec![
                        Event::EncryptedAmountsRemoved((*removed).try_into()?),
                        Event::NewEncryptedAmount((*added).try_into()?),
                    ]
                },
                AccountTransactionEffects::EncryptedAmountTransferredWithMemo {
                    removed,
                    added,
                    memo,
                } => {
                    vec![
                        Event::EncryptedAmountsRemoved((*removed).try_into()?),
                        Event::NewEncryptedAmount((*added).try_into()?),
                        Event::TransferMemo(memo.into()),
                    ]
                },
                AccountTransactionEffects::TransferredToEncrypted { data } => {
                    vec![Event::EncryptedSelfAmountAdded(EncryptedSelfAmountAdded {
                        account_address: data.account.into(),
                        new_encrypted_amount: serde_json::to_string(&data.new_amount)?,
                        amount: data.amount.micro_ccd.try_into()?,
                    })]
                },
                AccountTransactionEffects::TransferredToPublic { removed, amount } => {
                    vec![
                        Event::EncryptedAmountsRemoved((*removed).try_into()?),
                        Event::AmountAddedByDecryption(AmountAddedByDecryption {
                            amount: amount.micro_ccd().try_into()?,
                            account_address: details.sender.into(),
                        }),
                    ]
                },
                AccountTransactionEffects::TransferredWithSchedule { to, amount } => {
                    vec![Event::TransferredWithSchedule(TransferredWithSchedule {
                        from_account_address: details.sender.into(),
                        to_account_address: to.into(),
                        total_amount: amount
                            .into_iter()
                            .map(|(_, amount)| amount.micro_ccd())
                            .sum::<u64>()
                            .try_into()?,
                    })]
                },
                AccountTransactionEffects::TransferredWithScheduleAndMemo { to, amount, memo } => {
                    vec![
                        Event::TransferredWithSchedule(TransferredWithSchedule {
                            from_account_address: details.sender.into(),
                            to_account_address: to.into(),
                            total_amount: amount
                                .into_iter()
                                .map(|(_, amount)| amount.micro_ccd())
                                .sum::<u64>()
                                .try_into()?,
                        }),
                        Event::TransferMemo(memo.try_into()?),
                    ]
                },
                AccountTransactionEffects::CredentialKeysUpdated { cred_id } => {
                    vec![Event::CredentialKeysUpdated(CredentialKeysUpdated {
                        cred_id: cred_id.to_string(),
                    })]
                },
                AccountTransactionEffects::CredentialsUpdated {
                    new_cred_ids,
                    removed_cred_ids,
                    new_threshold,
                } => {
                    vec![Event::CredentialsUpdated(CredentialsUpdated {
                        account_address: details.sender.into(),
                        new_cred_ids: new_cred_ids
                            .into_iter()
                            .map(|cred| cred.to_string())
                            .collect(),
                        removed_cred_ids: removed_cred_ids
                            .into_iter()
                            .map(|cred| cred.to_string())
                            .collect(),
                        new_threshold: Byte(u8::from(new_threshold)),
                    })]
                },
                AccountTransactionEffects::DataRegistered { data } => {
                    vec![Event::DataRegistered(DataRegistered {
                        data_as_hex: hex::encode(data.as_ref()),
                        decoded: todo!(),
                    })]
                },
                AccountTransactionEffects::BakerConfigured { data } => {
                    data.into_iter()
                        .map(|baker_event| {
                            use concordium_rust_sdk::types::BakerEvent;
                            match baker_event {
                                BakerEvent::BakerAdded { data } => {
                                    Ok(Event::BakerAdded(BakerAdded {
                                        staked_amount: data.stake.micro_ccd.try_into()?,
                                        restake_earnings: data.restake_earnings,
                                        baker_id: data.keys_event.baker_id.id.index.try_into()?,
                                        sign_key: serde_json::to_string(&data.keys_event.sign_key)?,
                                        election_key: serde_json::to_string(
                                            &data.keys_event.election_key,
                                        )?,
                                        aggregation_key: serde_json::to_string(
                                            &data.keys_event.aggregation_key,
                                        )?,
                                    }))
                                },
                                BakerEvent::BakerRemoved { baker_id } => {
                                    Ok(Event::BakerRemoved(BakerRemoved {
                                        baker_id: baker_id.id.index.try_into()?,
                                    }))
                                },
                                BakerEvent::BakerStakeIncreased {
                                    baker_id,
                                    new_stake,
                                } => {
                                    Ok(Event::BakerStakeIncreased(BakerStakeIncreased {
                                        baker_id: baker_id.id.index.try_into()?,
                                        new_staked_amount: new_stake.micro_ccd.try_into()?,
                                    }))
                                },
                                BakerEvent::BakerStakeDecreased {
                                    baker_id,
                                    new_stake,
                                } => {
                                    Ok(Event::BakerStakeDecreased(BakerStakeDecreased {
                                        baker_id: baker_id.id.index.try_into()?,
                                        new_staked_amount: new_stake.micro_ccd.try_into()?,
                                    }))
                                },
                                BakerEvent::BakerRestakeEarningsUpdated {
                                    baker_id,
                                    restake_earnings,
                                } => {
                                    Ok(Event::BakerSetRestakeEarnings(BakerSetRestakeEarnings {
                                        baker_id: baker_id.id.index.try_into()?,
                                        restake_earnings,
                                    }))
                                },
                                BakerEvent::BakerKeysUpdated { data } => {
                                    Ok(Event::BakerKeysUpdated(BakerKeysUpdated {
                                        baker_id: data.baker_id.id.index.try_into()?,
                                        sign_key: serde_json::to_string(&data.sign_key)?,
                                        election_key: serde_json::to_string(&data.election_key)?,
                                        aggregation_key: serde_json::to_string(
                                            &data.aggregation_key,
                                        )?,
                                    }))
                                },
                                BakerEvent::BakerSetOpenStatus {
                                    baker_id,
                                    open_status,
                                } => {
                                    Ok(Event::BakerSetOpenStatus(BakerSetOpenStatus {
                                        baker_id: baker_id.id.index.try_into()?,
                                        account_address: details.sender.into(),
                                        open_status: open_status.into(),
                                    }))
                                },
                                BakerEvent::BakerSetMetadataURL {
                                    baker_id,
                                    metadata_url,
                                } => {
                                    Ok(Event::BakerSetMetadataURL(BakerSetMetadataURL {
                                        baker_id: baker_id.id.index.try_into()?,
                                        account_address: details.sender.into(),
                                        metadata_url: metadata_url.into(),
                                    }))
                                },
                                BakerEvent::BakerSetTransactionFeeCommission {
                                    baker_id,
                                    transaction_fee_commission,
                                } => {
                                    Ok(Event::BakerSetTransactionFeeCommission(
                                        BakerSetTransactionFeeCommission {
                                            baker_id: baker_id.id.index.try_into()?,
                                            account_address: details.sender.into(),
                                            transaction_fee_commission: todo!(),
                                        },
                                    ))
                                },
                                BakerEvent::BakerSetBakingRewardCommission {
                                    baker_id,
                                    baking_reward_commission,
                                } => {
                                    Ok(Event::BakerSetBakingRewardCommission(
                                        BakerSetBakingRewardCommission {
                                            baker_id: baker_id.id.index.try_into()?,
                                            account_address: details.sender.into(),
                                            baking_reward_commission: todo!(),
                                        },
                                    ))
                                },
                                BakerEvent::BakerSetFinalizationRewardCommission {
                                    baker_id,
                                    finalization_reward_commission,
                                } => {
                                    Ok(Event::BakerSetFinalizationRewardCommission(
                                        BakerSetFinalizationRewardCommission {
                                            baker_id: baker_id.id.index.try_into()?,
                                            account_address: details.sender.into(),
                                            finalization_reward_commission: todo!(),
                                        },
                                    ))
                                },
                            }
                        })
                        .collect::<anyhow::Result<Vec<Event>>>()?
                },
                AccountTransactionEffects::DelegationConfigured { data } => {
                    use concordium_rust_sdk::types::DelegationEvent;
                    data.into_iter()
                        .map(|event| {
                            match event {
                                DelegationEvent::DelegationStakeIncreased {
                                    delegator_id,
                                    new_stake,
                                } => {
                                    Ok(Event::DelegationStakeIncreased(DelegationStakeIncreased {
                                        delegator_id: delegator_id.id.index.try_into()?,
                                        account_address: details.sender.into(),
                                        new_staked_amount: new_stake.micro_ccd().try_into()?,
                                    }))
                                },
                                DelegationEvent::DelegationStakeDecreased {
                                    delegator_id,
                                    new_stake,
                                } => {
                                    Ok(Event::DelegationStakeDecreased(DelegationStakeDecreased {
                                        delegator_id: delegator_id.id.index.try_into()?,
                                        account_address: details.sender.into(),
                                        new_staked_amount: new_stake.micro_ccd().try_into()?,
                                    }))
                                },
                                DelegationEvent::DelegationSetRestakeEarnings {
                                    delegator_id,
                                    restake_earnings,
                                } => {
                                    Ok(Event::DelegationSetRestakeEarnings(
                                        DelegationSetRestakeEarnings {
                                            delegator_id: delegator_id.id.index.try_into()?,
                                            account_address: details.sender.into(),
                                            restake_earnings,
                                        },
                                    ))
                                },
                                DelegationEvent::DelegationSetDelegationTarget {
                                    delegator_id,
                                    delegation_target,
                                } => {
                                    Ok(Event::DelegationSetDelegationTarget(
                                        DelegationSetDelegationTarget {
                                            delegator_id: delegator_id.id.index.try_into()?,
                                            account_address: details.sender.into(),
                                            delegation_target: delegation_target.try_into()?,
                                        },
                                    ))
                                },
                                DelegationEvent::DelegationAdded { delegator_id } => {
                                    Ok(Event::DelegationAdded(DelegationAdded {
                                        delegator_id: delegator_id.id.index.try_into()?,
                                        account_address: details.sender.into(),
                                    }))
                                },
                                DelegationEvent::DelegationRemoved { delegator_id } => {
                                    Ok(Event::DelegationRemoved(DelegationRemoved {
                                        delegator_id: delegator_id.id.index.try_into()?,
                                        account_address: details.sender.into(),
                                    }))
                                },
                            }
                        })
                        .collect::<anyhow::Result<Vec<_>>>()?
                },
            }
        },
        BlockItemSummaryDetails::AccountCreation(details) => {
            vec![Event::AccountCreated(AccountCreated {
                account_address: details.address.into(),
            })]
        },
        BlockItemSummaryDetails::Update(details) => {
            vec![Event::ChainUpdateEnqueued(ChainUpdateEnqueued {
                effective_time: chrono::DateTime::from_timestamp(
                    details.effective_time.seconds.try_into()?,
                    0,
                )
                .context("Failed to parse effective time")?
                .naive_utc(),
                payload: true, // placeholder
            })]
        },
    };
    Ok(events)
}

impl From<concordium_rust_sdk::types::Memo> for TransferMemo {
    fn from(value: concordium_rust_sdk::types::Memo) -> Self {
        TransferMemo {
            decoded: todo!(),
            raw_hex: hex::encode(value.as_ref()),
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct Transferred {
    amount: Amount,
    from: AccountAddress,
    to: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct AccountCreated {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct AmountAddedByDecryption {
    amount: Amount,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
struct BakerAdded {
    staked_amount: Amount,
    restake_earnings: bool,
    baker_id: BakerId,
    sign_key: String,
    election_key: String,
    aggregation_key: String,
}
#[ComplexObject]
impl BakerAdded {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
struct BakerKeysUpdated {
    baker_id: BakerId,
    sign_key: String,
    election_key: String,
    aggregation_key: String,
}
#[ComplexObject]
impl BakerKeysUpdated {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
struct BakerRemoved {
    baker_id: BakerId,
}
#[ComplexObject]
impl BakerRemoved {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
struct BakerSetRestakeEarnings {
    baker_id: BakerId,
    restake_earnings: bool,
}
#[ComplexObject]
impl BakerSetRestakeEarnings {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
struct BakerStakeDecreased {
    baker_id: BakerId,
    new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeDecreased {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
struct BakerStakeIncreased {
    baker_id: BakerId,
    new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeIncreased {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractInitialized {
    module_ref: String,
    contract_address: ContractAddress,
    amount: Amount,
    init_name: String,
    version: ContractVersion,
    // TODO: eventsAsHex("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): StringConnection TODO: events("Returns the first
    // _n_ elements from the list." first: Int "Returns the elements in the list that come after
    // the specified cursor." after: String "Returns the last _n_ elements from the list." last:
    // Int "Returns the elements in the list that come before the specified cursor." before:
    // String): StringConnection
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
enum ContractVersion {
    V0,
    V1,
}

impl From<concordium_rust_sdk::types::smart_contracts::WasmVersion> for ContractVersion {
    fn from(value: concordium_rust_sdk::types::smart_contracts::WasmVersion) -> Self {
        use concordium_rust_sdk::types::smart_contracts::WasmVersion;
        match value {
            WasmVersion::V0 => ContractVersion::V0,
            WasmVersion::V1 => ContractVersion::V1,
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractModuleDeployed {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractCall {
    contract_updated: ContractUpdated,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct CredentialDeployed {
    reg_id: String,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct CredentialKeysUpdated {
    cred_id: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct CredentialsUpdated {
    account_address: AccountAddress,
    new_cred_ids: Vec<String>,
    removed_cred_ids: Vec<String>,
    new_threshold: Byte,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DataRegistered {
    decoded: DecodedText,
    data_as_hex: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DecodedText {
    text: String,
    decode_type: TextDecodeType,
}

#[derive(Enum, Copy, Clone, PartialEq, Eq, serde::Serialize, serde::Deserialize)]
enum TextDecodeType {
    Cbor,
    Hex,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct EncryptedAmountsRemoved {
    account_address: AccountAddress,
    new_encrypted_amount: String,
    input_amount: String,
    up_to_index: u64,
}

impl TryFrom<concordium_rust_sdk::types::EncryptedAmountRemovedEvent> for EncryptedAmountsRemoved {
    type Error = anyhow::Error;

    fn try_from(
        removed: concordium_rust_sdk::types::EncryptedAmountRemovedEvent,
    ) -> Result<Self, Self::Error> {
        Ok(EncryptedAmountsRemoved {
            account_address: removed.account.into(),
            new_encrypted_amount: serde_json::to_string(&removed.new_amount)?,
            input_amount: serde_json::to_string(&removed.input_amount)?,
            up_to_index: removed.up_to_index.index,
        })
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct EncryptedSelfAmountAdded {
    account_address: AccountAddress,
    new_encrypted_amount: String,
    amount: Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct NewEncryptedAmount {
    account_address: AccountAddress,
    new_index: u64,
    encrypted_amount: String,
}

impl TryFrom<concordium_rust_sdk::types::NewEncryptedAmountEvent> for NewEncryptedAmount {
    type Error = anyhow::Error;

    fn try_from(
        added: concordium_rust_sdk::types::NewEncryptedAmountEvent,
    ) -> Result<Self, Self::Error> {
        Ok(NewEncryptedAmount {
            account_address: added.receiver.into(),
            new_index: added.new_index.index,
            encrypted_amount: serde_json::to_string(&added.encrypted_amount)?,
        })
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct TransferMemo {
    decoded: DecodedText,
    raw_hex: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct TransferredWithSchedule {
    from_account_address: AccountAddress,
    to_account_address: AccountAddress,
    total_amount: Amount,
    // TODO: amountsSchedule("Returns the first _n_ elements from the list." first: Int "Returns
    // the elements in the list that come after the specified cursor." after: String "Returns the
    // last _n_ elements from the list." last: Int "Returns the elements in the list that come
    // before the specified cursor." before: String): AmountsScheduleConnection
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ModuleReferenceEvent {
    module_reference: String,
    sender: AccountAddress,
    block_height: BlockHeight,
    transaction_hash: String,
    block_slot_time: DateTime,
    display_schema: String,
    // TODO:
    // moduleReferenceRejectEvents(skip: Int take: Int):
    // ModuleReferenceRejectEventsCollectionSegment moduleReferenceContractLinkEvents(skip: Int
    // take: Int): ModuleReferenceContractLinkEventsCollectionSegment linkedContracts(skip: Int
    // take: Int): LinkedContractsCollectionSegment
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ChainUpdateEnqueued {
    effective_time: DateTime,
    // effective_immediately: bool, // Not sure this makes sense.
    payload: bool, // ChainUpdatePayload,
}

// union ChainUpdatePayload = MinBlockTimeUpdate | TimeoutParametersUpdate |
// FinalizationCommitteeParametersUpdate | BlockEnergyLimitUpdate | GasRewardsCpv2Update |
// ProtocolChainUpdatePayload | ElectionDifficultyChainUpdatePayload |
// EuroPerEnergyChainUpdatePayload | MicroCcdPerEuroChainUpdatePayload |
// FoundationAccountChainUpdatePayload | MintDistributionChainUpdatePayload |
// TransactionFeeDistributionChainUpdatePayload | GasRewardsChainUpdatePayload |
// BakerStakeThresholdChainUpdatePayload | RootKeysChainUpdatePayload | Level1KeysChainUpdatePayload
// | AddAnonymityRevokerChainUpdatePayload | AddIdentityProviderChainUpdatePayload |
// CooldownParametersChainUpdatePayload | PoolParametersChainUpdatePayload |
// TimeParametersChainUpdatePayload | MintDistributionV1ChainUpdatePayload
#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ChainUpdatePayload {
    todo: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractInterrupted {
    contract_address: ContractAddress,
    // eventsAsHex("Returns the first _n_ elements from the list." first: Int "Returns the elements
    // in the list that come after the specified cursor." after: String "Returns the last _n_
    // elements from the list." last: Int "Returns the elements in the list that come before the
    // specified cursor." before: String): StringConnection events("Returns the first _n_
    // elements from the list." first: Int "Returns the elements in the list that come after the
    // specified cursor." after: String "Returns the last _n_ elements from the list." last: Int
    // "Returns the elements in the list that come before the specified cursor." before: String):
    // StringConnection
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractResumed {
    contract_address: ContractAddress,
    success: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractUpdated {
    contract_address: ContractAddress,
    instigator: Address,
    amount: Amount,
    message_as_hex: String,
    receive_name: String,
    version: ContractVersion,
    // eventsAsHex("Returns the first _n_ elements from the list." first: Int "Returns the elements
    // in the list that come after the specified cursor." after: String "Returns the last _n_
    // elements from the list." last: Int "Returns the elements in the list that come before the
    // specified cursor." before: String): StringConnection events("Returns the first _n_
    // elements from the list." first: Int "Returns the elements in the list that come after the
    // specified cursor." after: String "Returns the last _n_ elements from the list." last: Int
    // "Returns the elements in the list that come before the specified cursor." before: String):
    // StringConnection
    message: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct ContractUpgraded {
    contract_address: ContractAddress,
    from: String,
    to: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerSetBakingRewardCommission {
    baker_id: BakerId,
    account_address: AccountAddress,
    baking_reward_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerSetFinalizationRewardCommission {
    baker_id: BakerId,
    account_address: AccountAddress,
    finalization_reward_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerSetTransactionFeeCommission {
    baker_id: BakerId,
    account_address: AccountAddress,
    transaction_fee_commission: Decimal,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerSetMetadataURL {
    baker_id: BakerId,
    account_address: AccountAddress,
    metadata_url: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct BakerSetOpenStatus {
    baker_id: BakerId,
    account_address: AccountAddress,
    open_status: BakerPoolOpenStatus,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DelegationAdded {
    delegator_id: AccountIndex,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DelegationRemoved {
    delegator_id: AccountIndex,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DelegationSetDelegationTarget {
    delegator_id: AccountIndex,
    account_address: AccountAddress,
    delegation_target: DelegationTarget,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DelegationSetRestakeEarnings {
    delegator_id: AccountIndex,
    account_address: AccountAddress,
    restake_earnings: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DelegationStakeDecreased {
    delegator_id: AccountIndex,
    account_address: AccountAddress,
    new_staked_amount: Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
struct DelegationStakeIncreased {
    delegator_id: AccountIndex,
    account_address: AccountAddress,
    new_staked_amount: Amount,
}

#[derive(SimpleObject)]
struct BlockMetrics {
    /// The most recent block height. Equals the total length of the chain minus one (genesis block
    /// is at height zero).
    last_block_height: BlockHeight,
    /// Total number of blocks added in requested period.
    blocks_added: i64,
    /// The average block time (slot-time difference between two adjacent blocks) in the requested
    /// period. Will be null if no blocks have been added in the requested period.
    avg_block_time: Option<f64>,
    // /// The average finalization time (slot-time difference between a given block and the block
    // that holds its finalization proof) in the requested period. Will be null if no blocks have
    // been finalized in the requested period. avg_finalization_time: Option<f32>,
    // /// The current total amount of CCD in existence.
    // last_total_micro_ccd: Amount,
    // /// The total CCD Released. This is total CCD supply not counting the balances of non
    // circulating accounts. last_total_micro_ccd_released: Option<Amount>,
    // /// The current total CCD released according to the Concordium promise published on
    // deck.concordium.com. Will be null for blocks with slot time before the published release
    // schedule. last_total_micro_ccd_unlocked: Option<Amount>,
    // /// The current total amount of CCD in encrypted balances.
    // last_total_micro_ccd_encrypted: Long,
    // /// The current total amount of CCD staked.
    // last_total_micro_ccd_staked: Long,
    // /// The current percentage of CCD released (of total CCD in existence) according to the
    // Concordium promise published on deck.concordium.com. Will be null for blocks with slot time
    // before the published release schedule." last_total_percentage_released: Option<f32>,
    // /// The current percentage of CCD encrypted (of total CCD in existence).
    // last_total_percentage_encrypted: f32,
    // /// The current percentage of CCD staked (of total CCD in existence).
    // last_total_percentage_staked: f32,
    // buckets: BlockMetricsBuckets,
}

#[derive(SimpleObject)]
struct BlockMetricsBuckets {
    /// The width (time interval) of each bucket.
    bucket_width: TimeSpan,
    /// Start of the bucket time period. Intended x-axis value.
    #[graphql(name = "x_Time")]
    x_time: Vec<DateTime>,
    /// Number of blocks added within the bucket time period. Intended y-axis value.
    #[graphql(name = "y_BlocksAdded")]
    y_blocks_added: Vec<i32>,
    /// The minimum block time (slot-time difference between two adjacent blocks) in the bucket
    /// period. Intended y-axis value. Will be null if no blocks have been added in the bucket
    /// period.
    #[graphql(name = "y_BlockTimeMin")]
    y_block_time_min: Vec<f32>,
    /// The average block time (slot-time difference between two adjacent blocks) in the bucket
    /// period. Intended y-axis value. Will be null if no blocks have been added in the bucket
    /// period.
    #[graphql(name = "y_BlockTimeAvg")]
    y_block_time_avg: Vec<f32>,
    /// The maximum block time (slot-time difference between two adjacent blocks) in the bucket
    /// period. Intended y-axis value. Will be null if no blocks have been added in the bucket
    /// period.
    #[graphql(name = "y_BlockTimeMax")]
    y_block_time_max: Vec<f32>,
    /// The minimum finalization time (slot-time difference between a given block and the block
    /// that holds its finalization proof) in the bucket period. Intended y-axis value. Will be
    /// null if no blocks have been finalized in the bucket period.
    #[graphql(name = "y_FinalizationTimeMin")]
    y_finalization_time_min: Vec<f32>,
    /// The average finalization time (slot-time difference between a given block and the block
    /// that holds its finalization proof) in the bucket period. Intended y-axis value. Will be
    /// null if no blocks have been finalized in the bucket period.
    #[graphql(name = "y_FinalizationTimeAvg")]
    y_finalization_time_avg: Vec<f32>,
    /// The maximum finalization time (slot-time difference between a given block and the block
    /// that holds its finalization proof) in the bucket period. Intended y-axis value. Will be
    /// null if no blocks have been finalized in the bucket period.
    #[graphql(name = "y_FinalizationTimeMax")]
    y_finalization_time_max: Vec<f32>,
    /// The total amount of CCD in existence at the end of the bucket period. Intended y-axis
    /// value.
    #[graphql(name = "y_LastTotalMicroCcd")]
    y_last_total_micro_ccd: Vec<Long>,
    /// The minimum amount of CCD in encrypted balances in the bucket period. Intended y-axis
    /// value. Will be null if no blocks have been added in the bucket period.
    #[graphql(name = "y_MinTotalMicroCcdEncrypted")]
    y_min_total_micro_ccd_encrypted: Vec<Long>,
    /// The maximum amount of CCD in encrypted balances in the bucket period. Intended y-axis
    /// value. Will be null if no blocks have been added in the bucket period.
    #[graphql(name = "y_MaxTotalMicroCcdEncrypted")]
    y_max_total_micro_ccd_encrypted: Vec<Long>,
    /// The total amount of CCD in encrypted balances at the end of the bucket period. Intended
    /// y-axis value.
    #[graphql(name = "y_LastTotalMicroCcdEncrypted")]
    y_last_total_micro_ccd_encrypted: Vec<Long>,
    /// The minimum amount of CCD staked in the bucket period. Intended y-axis value. Will be null
    /// if no blocks have been added in the bucket period.
    #[graphql(name = "y_MinTotalMicroCcdStaked")]
    y_min_total_micro_ccd_staked: Vec<Long>,
    /// The maximum amount of CCD staked in the bucket period. Intended y-axis value. Will be null
    /// if no blocks have been added in the bucket period.
    #[graphql(name = "y_MaxTotalMicroCcdStaked")]
    y_max_total_micro_ccd_staked: Vec<Long>,
    /// The total amount of CCD staked at the end of the bucket period. Intended y-axis value.
    #[graphql(name = "y_LastTotalMicroCcdStaked")]
    y_last_total_micro_ccd_staked: Vec<Long>,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq)]
enum MetricsPeriod {
    LastHour,
    Last24Hours,
    Last7Days,
    Last30Days,
    LastYear,
}

#[derive(sqlx::Type)]
#[sqlx(type_name = "transaction_type")] // only for PostgreSQL to match a type definition
pub enum DbTransactionType {
    Account,
    CredentialDeployment,
    Update,
}
