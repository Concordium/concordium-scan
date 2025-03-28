export type Maybe<T> = T | null;
export type InputMaybe<T> = Maybe<T>;
export type Exact<T extends { [key: string]: unknown }> = { [K in keyof T]: T[K] };
export type MakeOptional<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]?: Maybe<T[SubKey]> };
export type MakeMaybe<T, K extends keyof T> = Omit<T, K> & { [SubKey in K]: Maybe<T[SubKey]> };
/** All built-in and custom scalars, mapped to their actual values */
export type Scalars = {
  ID: string;
  String: string;
  Boolean: boolean;
  Int: number;
  Float: number;
  BigInteger: any;
  /** The `Byte` scalar type represents non-fractional whole numeric values. Byte can represent values between 0 and 255. */
  Byte: any;
  /** The `DateTime` scalar represents an ISO-8601 compliant date time type. */
  DateTime: any;
  /** The built-in `Decimal` scalar type. */
  Decimal: any;
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: any;
  /** The `TimeSpan` scalar represents an ISO-8601 compliant duration type. */
  TimeSpan: any;
  /** The UnsignedInt scalar type represents a unsigned 32-bit numeric non-fractional value greater than or equal to 0. */
  UnsignedInt: any;
  /** The UnsignedLong scalar type represents a unsigned 64-bit numeric non-fractional value greater than or equal to 0. */
  UnsignedLong: any;
};

export type Account = {
  __typename?: 'Account';
  accountStatement?: Maybe<AccountStatementEntryConnection>;
  address: AccountAddress;
  amount: Scalars['UnsignedLong'];
  baker?: Maybe<Baker>;
  createdAt: Scalars['DateTime'];
  delegation?: Maybe<Delegation>;
  id: Scalars['ID'];
  releaseSchedule: AccountReleaseSchedule;
  rewards?: Maybe<AccountRewardConnection>;
  tokens?: Maybe<AccountTokenConnection>;
  transactionCount: Scalars['Int'];
  transactions?: Maybe<AccountTransactionRelationConnection>;
};


export type AccountAccountStatementArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type AccountRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type AccountTokensArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type AccountTransactionsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type AccountAddress = {
  __typename?: 'AccountAddress';
  asString: Scalars['String'];
};

export type AccountAddressAmount = {
  __typename?: 'AccountAddressAmount';
  accountAddress: AccountAddress;
  amount: Scalars['UnsignedLong'];
};

/** A connection to a list of items. */
export type AccountAddressAmountConnection = {
  __typename?: 'AccountAddressAmountConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountAddressAmountEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<AccountAddressAmount>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountAddressAmountEdge = {
  __typename?: 'AccountAddressAmountEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: AccountAddressAmount;
};

export type AccountCreated = {
  __typename?: 'AccountCreated';
  accountAddress: AccountAddress;
};

export type AccountFilterInput = {
  isDelegator?: InputMaybe<Scalars['Boolean']>;
};

export type AccountReleaseSchedule = {
  __typename?: 'AccountReleaseSchedule';
  schedule?: Maybe<AccountReleaseScheduleItemConnection>;
  totalAmount: Scalars['UnsignedLong'];
};


export type AccountReleaseScheduleScheduleArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type AccountReleaseScheduleItem = {
  __typename?: 'AccountReleaseScheduleItem';
  amount: Scalars['UnsignedLong'];
  timestamp: Scalars['DateTime'];
  transaction: Transaction;
};

/** A connection to a list of items. */
export type AccountReleaseScheduleItemConnection = {
  __typename?: 'AccountReleaseScheduleItemConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountReleaseScheduleItemEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<AccountReleaseScheduleItem>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountReleaseScheduleItemEdge = {
  __typename?: 'AccountReleaseScheduleItemEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: AccountReleaseScheduleItem;
};

export type AccountReward = {
  __typename?: 'AccountReward';
  amount: Scalars['UnsignedLong'];
  block: Block;
  id: Scalars['ID'];
  rewardType: RewardType;
  timestamp: Scalars['DateTime'];
};

/** A connection to a list of items. */
export type AccountRewardConnection = {
  __typename?: 'AccountRewardConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountRewardEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<AccountReward>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountRewardEdge = {
  __typename?: 'AccountRewardEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: AccountReward;
};

export enum AccountSort {
  AgeAsc = 'AGE_ASC',
  AgeDesc = 'AGE_DESC',
  AmountAsc = 'AMOUNT_ASC',
  AmountDesc = 'AMOUNT_DESC',
  DelegatedStakeAsc = 'DELEGATED_STAKE_ASC',
  DelegatedStakeDesc = 'DELEGATED_STAKE_DESC',
  TransactionCountAsc = 'TRANSACTION_COUNT_ASC',
  TransactionCountDesc = 'TRANSACTION_COUNT_DESC'
}

export type AccountStatementEntry = {
  __typename?: 'AccountStatementEntry';
  accountBalance: Scalars['UnsignedLong'];
  amount: Scalars['Long'];
  entryType: AccountStatementEntryType;
  id: Scalars['ID'];
  reference: BlockOrTransaction;
  timestamp: Scalars['DateTime'];
};

/** A connection to a list of items. */
export type AccountStatementEntryConnection = {
  __typename?: 'AccountStatementEntryConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountStatementEntryEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<AccountStatementEntry>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountStatementEntryEdge = {
  __typename?: 'AccountStatementEntryEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: AccountStatementEntry;
};

export enum AccountStatementEntryType {
  AmountDecrypted = 'AMOUNT_DECRYPTED',
  AmountEncrypted = 'AMOUNT_ENCRYPTED',
  BakerReward = 'BAKER_REWARD',
  FinalizationReward = 'FINALIZATION_REWARD',
  FoundationReward = 'FOUNDATION_REWARD',
  TransactionFee = 'TRANSACTION_FEE',
  TransactionFeeReward = 'TRANSACTION_FEE_REWARD',
  TransferIn = 'TRANSFER_IN',
  TransferOut = 'TRANSFER_OUT'
}

export type AccountToken = {
  __typename?: 'AccountToken';
  account: Account;
  accountId: Scalars['Long'];
  balance: Scalars['BigInteger'];
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  token: Token;
  tokenId: Scalars['String'];
};

/** A connection to a list of items. */
export type AccountTokenConnection = {
  __typename?: 'AccountTokenConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountTokenEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<AccountToken>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountTokenEdge = {
  __typename?: 'AccountTokenEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: AccountToken;
};

export type AccountTransaction = {
  __typename?: 'AccountTransaction';
  accountTransactionType?: Maybe<AccountTransactionType>;
};

export type AccountTransactionRelation = {
  __typename?: 'AccountTransactionRelation';
  transaction: Transaction;
};

/** A connection to a list of items. */
export type AccountTransactionRelationConnection = {
  __typename?: 'AccountTransactionRelationConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountTransactionRelationEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<AccountTransactionRelation>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountTransactionRelationEdge = {
  __typename?: 'AccountTransactionRelationEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: AccountTransactionRelation;
};

/** Types of account transactions. */
export enum AccountTransactionType {
  /** Register an account as a baker. */
  AddBaker = 'ADD_BAKER',
  /** Configure an account's baker. */
  ConfigureBaker = 'CONFIGURE_BAKER',
  /** Configure an account's stake delegation. */
  ConfigureDelegation = 'CONFIGURE_DELEGATION',
  /** Deploy a Wasm module. */
  DeployModule = 'DEPLOY_MODULE',
  /** Transfer encrypted amount. */
  EncryptedTransfer = 'ENCRYPTED_TRANSFER',
  /** Same as encrypted transfer, but with a memo. */
  EncryptedTransferWithMemo = 'ENCRYPTED_TRANSFER_WITH_MEMO',
  /** Initialize a smart contract instance. */
  InitializeSmartContractInstance = 'INITIALIZE_SMART_CONTRACT_INSTANCE',
  /** Register some data on the chain. */
  RegisterData = 'REGISTER_DATA',
  /** Remove an account as a baker. */
  RemoveBaker = 'REMOVE_BAKER',
  /** Transfer CCD from an account to another. */
  SimpleTransfer = 'SIMPLE_TRANSFER',
  /** Same as transfer, but with a memo field. */
  SimpleTransferWithMemo = 'SIMPLE_TRANSFER_WITH_MEMO',
  /** Transfer from public to encrypted balance of the same account. */
  TransferToEncrypted = 'TRANSFER_TO_ENCRYPTED',
  /** Transfer from encrypted to public balance of the same account. */
  TransferToPublic = 'TRANSFER_TO_PUBLIC',
  /** Transfer a CCD with a release schedule. */
  TransferWithSchedule = 'TRANSFER_WITH_SCHEDULE',
  /** Same as transfer with schedule, but with an added memo. */
  TransferWithScheduleWithMemo = 'TRANSFER_WITH_SCHEDULE_WITH_MEMO',
  /** Update baker keys */
  UpdateBakerKeys = 'UPDATE_BAKER_KEYS',
  /** Update whether the baker automatically restakes earnings. */
  UpdateBakerRestakeEarnings = 'UPDATE_BAKER_RESTAKE_EARNINGS',
  /** Update the staked amount. */
  UpdateBakerStake = 'UPDATE_BAKER_STAKE',
  /** Update the account's credentials. */
  UpdateCredentials = 'UPDATE_CREDENTIALS',
  /** Update given credential keys */
  UpdateCredentialKeys = 'UPDATE_CREDENTIAL_KEYS',
  /** Update a smart contract instance. */
  UpdateSmartContractInstance = 'UPDATE_SMART_CONTRACT_INSTANCE'
}

/** A segment of a collection. */
export type AccountsCollectionSegment = {
  __typename?: 'AccountsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<AccountToken>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

/** A connection to a list of items. */
export type AccountsConnection = {
  __typename?: 'AccountsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AccountsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Account>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountsEdge = {
  __typename?: 'AccountsEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Account;
};

export type AccountsMetrics = {
  __typename?: 'AccountsMetrics';
  /** Total number of accounts created in requested period. */
  accountsCreated: Scalars['Int'];
  buckets: AccountsMetricsBuckets;
  /** Total number of accounts created (all time) */
  lastCumulativeAccountsCreated: Scalars['Long'];
};

export type AccountsMetricsBuckets = {
  __typename?: 'AccountsMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** Number of accounts created within bucket time period. Intended y-axis value. */
  y_AccountsCreated: Array<Scalars['Int']>;
  /** Total number of accounts created (all time) at the end of the bucket period. Intended y-axis value. */
  y_LastCumulativeAccountsCreated: Array<Scalars['Long']>;
};

/** Structure used to send messages to the Subscribers */
export type AccountsUpdatedSubscriptionItem = {
  __typename?: 'AccountsUpdatedSubscriptionItem';
  /** Account Address */
  address: Scalars['String'];
};

export type ActiveBakerState = {
  __typename?: 'ActiveBakerState';
  /** The status of the bakers node. Will be null if no status for the node exists. */
  nodeStatus?: Maybe<NodeStatus>;
  pendingChange?: Maybe<PendingBakerChange>;
  pool?: Maybe<BakerPool>;
  restakeEarnings: Scalars['Boolean'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type AddAnonymityRevokerChainUpdatePayload = {
  __typename?: 'AddAnonymityRevokerChainUpdatePayload';
  arIdentity: Scalars['Int'];
  description: Scalars['String'];
  name: Scalars['String'];
  url: Scalars['String'];
};

export type AddIdentityProviderChainUpdatePayload = {
  __typename?: 'AddIdentityProviderChainUpdatePayload';
  description: Scalars['String'];
  ipIdentity: Scalars['Int'];
  name: Scalars['String'];
  url: Scalars['String'];
};

export type Address = AccountAddress | ContractAddress;

export type AlreadyABaker = {
  __typename?: 'AlreadyABaker';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  bakerId: Scalars['UnsignedLong'];
};

export type AlreadyADelegator = {
  __typename?: 'AlreadyADelegator';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type AmountAddedByDecryption = {
  __typename?: 'AmountAddedByDecryption';
  accountAddress: AccountAddress;
  amount: Scalars['UnsignedLong'];
};

export type AmountTooLarge = {
  __typename?: 'AmountTooLarge';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  address: Address;
  amount: Scalars['UnsignedLong'];
};

/** A connection to a list of items. */
export type AmountsScheduleConnection = {
  __typename?: 'AmountsScheduleConnection';
  /** A list of edges. */
  edges?: Maybe<Array<AmountsScheduleEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<TimestampedAmount>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AmountsScheduleEdge = {
  __typename?: 'AmountsScheduleEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: TimestampedAmount;
};

export enum ApyPeriod {
  Last7Days = 'LAST7_DAYS',
  Last30Days = 'LAST30_DAYS'
}

export type Baker = {
  __typename?: 'Baker';
  account: Account;
  bakerId: Scalars['Long'];
  id: Scalars['ID'];
  state: BakerState;
  /** Get the transactions that have affected the baker. */
  transactions?: Maybe<BakerTransactionRelationConnection>;
};


export type BakerTransactionsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type BakerAdded = {
  __typename?: 'BakerAdded';
  accountAddress: AccountAddress;
  aggregationKey: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
  electionKey: Scalars['String'];
  restakeEarnings: Scalars['Boolean'];
  signKey: Scalars['String'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type BakerDelegationTarget = {
  __typename?: 'BakerDelegationTarget';
  bakerId: Scalars['Long'];
};

export type BakerFilterInput = {
  includeRemoved?: InputMaybe<Scalars['Boolean']>;
  openStatusFilter?: InputMaybe<BakerPoolOpenStatus>;
};

export type BakerInCooldown = {
  __typename?: 'BakerInCooldown';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type BakerKeysUpdated = {
  __typename?: 'BakerKeysUpdated';
  accountAddress: AccountAddress;
  aggregationKey: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
  electionKey: Scalars['String'];
  signKey: Scalars['String'];
};

export type BakerMetrics = {
  __typename?: 'BakerMetrics';
  /** Bakers added in requested period */
  bakersAdded: Scalars['Int'];
  /** Bakers removed in requested period */
  bakersRemoved: Scalars['Int'];
  buckets: BakerMetricsBuckets;
  /** Current number of bakers */
  lastBakerCount: Scalars['Int'];
};

export type BakerMetricsBuckets = {
  __typename?: 'BakerMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** Number of bakers added within bucket time period. Intended y-axis value. */
  y_BakersAdded: Array<Scalars['Int']>;
  /** Number of bakers removed within bucket time period. Intended y-axis value. */
  y_BakersRemoved: Array<Scalars['Int']>;
  /** Number of bakers at the end of the bucket period. Intended y-axis value. */
  y_LastBakerCount: Array<Scalars['Int']>;
};

export type BakerPool = {
  __typename?: 'BakerPool';
  apy: PoolApy;
  commissionRates: CommissionRates;
  /** The total amount staked by delegation to this baker pool. */
  delegatedStake: Scalars['UnsignedLong'];
  /** The maximum amount that may be delegated to the pool, accounting for leverage and stake limits. */
  delegatedStakeCap: Scalars['UnsignedLong'];
  delegatorCount: Scalars['Int'];
  delegators?: Maybe<DelegatorsConnection>;
  lotteryPower?: Maybe<Scalars['Decimal']>;
  metadataUrl: Scalars['String'];
  openStatus: BakerPoolOpenStatus;
  paydayCommissionRates?: Maybe<CommissionRates>;
  selfSuspended?:Maybe<Scalars['UnsignedLong']>,
  inactiveSuspended?:Maybe<Scalars['UnsignedLong']>,
  primedForSuspension?:Maybe<Scalars['UnsignedLong']>,
  poolRewards?: Maybe<PaydayPoolRewardConnection>;
  /** Ranking of the baker pool by total staked amount. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition. */
  rankingByTotalStake?: Maybe<Ranking>;
  /** The total amount staked in this baker pool. Includes both baker stake and delegated stake. */
  totalStake: Scalars['UnsignedLong'];
  /** Total stake of the baker pool as a percentage of all CCDs in existence. Value may be null for brand new bakers where statistics have not been calculated yet. This should be rare and only a temporary condition. */
  totalStakePercentage?: Maybe<Scalars['Decimal']>;
};


export type BakerPoolApyArgs = {
  period: ApyPeriod;
};


export type BakerPoolDelegatorsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type BakerPoolPoolRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export enum BakerPoolOpenStatus {
  ClosedForAll = 'CLOSED_FOR_ALL',
  ClosedForNew = 'CLOSED_FOR_NEW',
  OpenForAll = 'OPEN_FOR_ALL'
}

export type BakerPoolRewardTarget = {
  __typename?: 'BakerPoolRewardTarget';
  bakerId: Scalars['Long'];
};

export type BakerRemoved = {
  __typename?: 'BakerRemoved';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
};

export type BakerSetBakingRewardCommission = {
  __typename?: 'BakerSetBakingRewardCommission';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  bakingRewardCommission: Scalars['Decimal'];
};

export type BakerSetFinalizationRewardCommission = {
  __typename?: 'BakerSetFinalizationRewardCommission';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  finalizationRewardCommission: Scalars['Decimal'];
};

export type BakerSetMetadataUrl = {
  __typename?: 'BakerSetMetadataURL';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  metadataUrl: Scalars['String'];
};

export type BakerSetOpenStatus = {
  __typename?: 'BakerSetOpenStatus';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  openStatus: BakerPoolOpenStatus;
};

export type BakerSetRestakeEarnings = {
  __typename?: 'BakerSetRestakeEarnings';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  restakeEarnings: Scalars['Boolean'];
};

export type BakerSetTransactionFeeCommission = {
  __typename?: 'BakerSetTransactionFeeCommission';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  transactionFeeCommission: Scalars['Decimal'];
};

export enum BakerSort {
  BakerApy30DaysDesc = 'BAKER_APY30_DAYS_DESC',
  BakerIdAsc = 'BAKER_ID_ASC',
  BakerIdDesc = 'BAKER_ID_DESC',
  BakerStakedAmountAsc = 'BAKER_STAKED_AMOUNT_ASC',
  BakerStakedAmountDesc = 'BAKER_STAKED_AMOUNT_DESC',
  BlockCommissionsAsc = 'BLOCK_COMMISSIONS_ASC',
  BlockCommissionsDesc = 'BLOCK_COMMISSIONS_DESC',
  DelegatorApy30DaysDesc = 'DELEGATOR_APY30_DAYS_DESC',
  DelegatorCountAsc = 'DELEGATOR_COUNT_ASC',
  DelegatorCountDesc = 'DELEGATOR_COUNT_DESC',
  TotalStakedAmountAsc = 'TOTAL_STAKED_AMOUNT_ASC',
  TotalStakedAmountDesc = 'TOTAL_STAKED_AMOUNT_DESC'
}

export type BakerStakeDecreased = {
  __typename?: 'BakerStakeDecreased';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type BakerStakeIncreased = {
  __typename?: 'BakerStakeIncreased';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type BakerStakeThresholdChainUpdatePayload = {
  __typename?: 'BakerStakeThresholdChainUpdatePayload';
  amount: Scalars['UnsignedLong'];
};

export type BakerState = ActiveBakerState | RemovedBakerState;

export type BakerTransactionRelation = {
  __typename?: 'BakerTransactionRelation';
  id: Scalars['ID'];
  transaction: Transaction;
};

/** A connection to a list of items. */
export type BakerTransactionRelationConnection = {
  __typename?: 'BakerTransactionRelationConnection';
  /** A list of edges. */
  edges?: Maybe<Array<BakerTransactionRelationEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<BakerTransactionRelation>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type BakerTransactionRelationEdge = {
  __typename?: 'BakerTransactionRelationEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: BakerTransactionRelation;
};

/** A connection to a list of items. */
export type BakersConnection = {
  __typename?: 'BakersConnection';
  /** A list of edges. */
  edges?: Maybe<Array<BakersEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Baker>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type BakersEdge = {
  __typename?: 'BakersEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Baker;
};

export type BakingRewardCommissionNotInRange = {
  __typename?: 'BakingRewardCommissionNotInRange';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type BakingRewardsSpecialEvent = {
  __typename?: 'BakingRewardsSpecialEvent';
  bakingRewards?: Maybe<AccountAddressAmountConnection>;
  id: Scalars['ID'];
  remainder: Scalars['UnsignedLong'];
};


export type BakingRewardsSpecialEventBakingRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type BalanceStatistics = {
  __typename?: 'BalanceStatistics';
  /** The amount in the baking reward account */
  bakingRewardAccount: Scalars['UnsignedLong'];
  /** The amount in the finalization reward account */
  finalizationRewardAccount: Scalars['UnsignedLong'];
  /** The amount in the GAS account */
  gasAccount: Scalars['UnsignedLong'];
  /** The total CCD in existence */
  totalAmount: Scalars['UnsignedLong'];
  /** The total CCD in encrypted balances */
  totalAmountEncrypted: Scalars['UnsignedLong'];
  /** The total CCD locked in release schedules (from transfers with schedule) */
  totalAmountLockedInReleaseSchedules: Scalars['UnsignedLong'];
  /** The total CCD Released. This is total CCD supply not counting the balances of non circulating accounts */
  totalAmountReleased?: Maybe<Scalars['UnsignedLong']>;
  /** The total CCD staked */
  totalAmountStaked: Scalars['UnsignedLong'];
  /** The total CCD Unlocked according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule. */
  totalAmountUnlocked?: Maybe<Scalars['UnsignedLong']>;
};

export type Block = {
  __typename?: 'Block';
  bakerId?: Maybe<Scalars['Int']>;
  balanceStatistics: BalanceStatistics;
  blockHash: Scalars['String'];
  blockHeight: Scalars['Int'];
  blockSlotTime: Scalars['DateTime'];
  blockStatistics: BlockStatistics;
  chainParameters: ChainParameters;
  finalized: Scalars['Boolean'];
  id: Scalars['ID'];
  specialEvents?: Maybe<SpecialEventsConnection>;
  transactionCount: Scalars['Int'];
  transactions?: Maybe<TransactionsConnection>;
};


export type BlockSpecialEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  includeFilter?: InputMaybe<Array<SpecialEventTypeFilter>>;
  last?: InputMaybe<Scalars['Int']>;
};


export type BlockTransactionsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type BlockAccrueRewardSpecialEvent = {
  __typename?: 'BlockAccrueRewardSpecialEvent';
  /** The baker of the block, who will receive the award. */
  bakerId: Scalars['UnsignedLong'];
  /** The amount awarded to the baker. */
  bakerReward: Scalars['UnsignedLong'];
  /** The amount awarded to the foundation. */
  foundationCharge: Scalars['UnsignedLong'];
  id: Scalars['ID'];
  /** The new balance of the GAS account. */
  newGasAccount: Scalars['UnsignedLong'];
  /** The old balance of the GAS account. */
  oldGasAccount: Scalars['UnsignedLong'];
  /** The amount awarded to the passive delegators. */
  passiveReward: Scalars['UnsignedLong'];
  /** The total fees paid for transactions in the block. */
  transactionFees: Scalars['UnsignedLong'];
};

export type BlockEnergyLimitUpdate = {
  __typename?: 'BlockEnergyLimitUpdate';
  energyLimit: Scalars['UnsignedLong'];
};

export type BlockMetrics = {
  __typename?: 'BlockMetrics';
  /** The average block time (slot-time difference between two adjacent blocks) in the requested period. Will be null if no blocks have been added in the requested period. */
  avgBlockTime?: Maybe<Scalars['Float']>;
  /** The average finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the requested period. Will be null if no blocks have been finalized in the requested period. */
  avgFinalizationTime?: Maybe<Scalars['Float']>;
  /** Total number of blocks added in requested period. */
  blocksAdded: Scalars['Int'];
  buckets: BlockMetricsBuckets;
  /** The most recent block height. Equals the total length of the chain minus one (genesis block is at height zero). */
  lastBlockHeight: Scalars['Long'];
  /** The current total amount of CCD in existence. */
  lastTotalMicroCcd: Scalars['Long'];
  /** The current total amount of CCD in encrypted balances. */
  lastTotalMicroCcdEncrypted: Scalars['Long'];
  /** The total CCD Released. This is total CCD supply not counting the balances of non circulating accounts */
  lastTotalMicroCcdReleased?: Maybe<Scalars['Long']>;
  /** The current total amount of CCD staked. */
  lastTotalMicroCcdStaked: Scalars['Long'];
  /** The current total CCD released according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule. */
  lastTotalMicroCcdUnlocked?: Maybe<Scalars['Long']>;
  /** The current percentage of CCD encrypted (of total CCD in existence) */
  lastTotalPercentageEncrypted: Scalars['Float'];
  /** The current percentage of CCD released (of total CCD in existence) according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule. */
  lastTotalPercentageReleased?: Maybe<Scalars['Float']>;
  /** The current percentage of CCD staked (of total CCD in existence) */
  lastTotalPercentageStaked: Scalars['Float'];
};

export type BlockMetricsBuckets = {
  __typename?: 'BlockMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** The average block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_BlockTimeAvg: Array<Maybe<Scalars['Float']>>;
  /** The maximum block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_BlockTimeMax: Array<Maybe<Scalars['Float']>>;
  /** The minimum block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_BlockTimeMin: Array<Maybe<Scalars['Float']>>;
  /** Number of blocks added within the bucket time period. Intended y-axis value. */
  y_BlocksAdded: Array<Scalars['Int']>;
  /** The average finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the bucket period. Intended y-axis value. Will be null if no blocks have been finalized in the bucket period. */
  y_FinalizationTimeAvg: Array<Maybe<Scalars['Float']>>;
  /** The maximum finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the bucket period. Intended y-axis value. Will be null if no blocks have been finalized in the bucket period. */
  y_FinalizationTimeMax: Array<Maybe<Scalars['Float']>>;
  /** The minimum finalization time (slot-time difference between a given block and the block that holds its finalization proof) in the bucket period. Intended y-axis value. Will be null if no blocks have been finalized in the bucket period. */
  y_FinalizationTimeMin: Array<Maybe<Scalars['Float']>>;
  /** The total amount of CCD in existence at the end of the bucket period. Intended y-axis value. */
  y_LastTotalMicroCcd: Array<Scalars['Long']>;
  /** The total amount of CCD in encrypted balances at the end of the bucket period. Intended y-axis value. */
  y_LastTotalMicroCcdEncrypted: Array<Scalars['Long']>;
  /** The total amount of CCD staked at the end of the bucket period. Intended y-axis value. */
  y_LastTotalMicroCcdStaked: Array<Scalars['Long']>;
  /** The maximum amount of CCD in encrypted balances in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_MaxTotalMicroCcdEncrypted: Array<Maybe<Scalars['Long']>>;
  /** The maximum amount of CCD staked in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_MaxTotalMicroCcdStaked: Array<Scalars['Long']>;
  /** The minimum amount of CCD in encrypted balances in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_MinTotalMicroCcdEncrypted: Array<Maybe<Scalars['Long']>>;
  /** The minimum amount of CCD staked in the bucket period. Intended y-axis value. Will be null if no blocks have been added in the bucket period. */
  y_MinTotalMicroCcdStaked: Array<Scalars['Long']>;
};

export type BlockOrTransaction = Block | Transaction;

export type BlockRewardsSpecialEvent = {
  __typename?: 'BlockRewardsSpecialEvent';
  bakerAccountAddress: AccountAddress;
  bakerReward: Scalars['UnsignedLong'];
  foundationAccountAddress: AccountAddress;
  foundationCharge: Scalars['UnsignedLong'];
  id: Scalars['ID'];
  newGasAccount: Scalars['UnsignedLong'];
  oldGasAccount: Scalars['UnsignedLong'];
  transactionFees: Scalars['UnsignedLong'];
};

export type BlockStatistics = {
  __typename?: 'BlockStatistics';
  blockTime: Scalars['Float'];
  finalizationTime?: Maybe<Scalars['Float']>;
};

/** A connection to a list of items. */
export type BlocksConnection = {
  __typename?: 'BlocksConnection';
  /** A list of edges. */
  edges?: Maybe<Array<BlocksEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Block>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type BlocksEdge = {
  __typename?: 'BlocksEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Block;
};

export type ChainParameters = {
  accountCreationLimit: Scalars['Int'];
  euroPerEnergy: ExchangeRate;
  foundationAccountAddress: AccountAddress;
  microCcdPerEuro: ExchangeRate;
};

export type ChainParametersV0 = ChainParameters & {
  __typename?: 'ChainParametersV0';
  accountCreationLimit: Scalars['Int'];
  bakerCooldownEpochs: Scalars['UnsignedLong'];
  electionDifficulty: Scalars['Decimal'];
  euroPerEnergy: ExchangeRate;
  foundationAccountAddress: AccountAddress;
  microCcdPerEuro: ExchangeRate;
  minimumThresholdForBaking: Scalars['UnsignedLong'];
  rewardParameters: RewardParametersV0;
};

export type ChainParametersV1 = ChainParameters & {
  __typename?: 'ChainParametersV1';
  accountCreationLimit: Scalars['Int'];
  bakingCommissionRange: CommissionRange;
  capitalBound: Scalars['Decimal'];
  delegatorCooldown: Scalars['UnsignedLong'];
  electionDifficulty: Scalars['Decimal'];
  euroPerEnergy: ExchangeRate;
  finalizationCommissionRange: CommissionRange;
  foundationAccountAddress: AccountAddress;
  leverageBound: LeverageFactor;
  microCcdPerEuro: ExchangeRate;
  minimumEquityCapital: Scalars['UnsignedLong'];
  mintPerPayday: Scalars['Decimal'];
  passiveBakingCommission: Scalars['Decimal'];
  passiveFinalizationCommission: Scalars['Decimal'];
  passiveTransactionCommission: Scalars['Decimal'];
  poolOwnerCooldown: Scalars['UnsignedLong'];
  rewardParameters: RewardParametersV1;
  rewardPeriodLength: Scalars['UnsignedLong'];
  transactionCommissionRange: CommissionRange;
};

export type ChainParametersV2 = ChainParameters & {
  __typename?: 'ChainParametersV2';
  accountCreationLimit: Scalars['Int'];
  bakingCommissionRange: CommissionRange;
  capitalBound: Scalars['Decimal'];
  delegatorCooldown: Scalars['UnsignedLong'];
  euroPerEnergy: ExchangeRate;
  finalizationCommissionRange: CommissionRange;
  foundationAccountAddress: AccountAddress;
  leverageBound: LeverageFactor;
  microCcdPerEuro: ExchangeRate;
  minimumEquityCapital: Scalars['UnsignedLong'];
  mintPerPayday: Scalars['Decimal'];
  passiveBakingCommission: Scalars['Decimal'];
  passiveFinalizationCommission: Scalars['Decimal'];
  passiveTransactionCommission: Scalars['Decimal'];
  poolOwnerCooldown: Scalars['UnsignedLong'];
  rewardParameters: RewardParametersV2;
  rewardPeriodLength: Scalars['UnsignedLong'];
  transactionCommissionRange: CommissionRange;
};

export type ChainParametersV3 = ChainParameters & {
  __typename?: 'ChainParametersV3';
  accountCreationLimit: Scalars['Int'];
  bakingCommissionRange: CommissionRange;
  capitalBound: Scalars['Decimal'];
  delegatorCooldown: Scalars['UnsignedLong'];
  euroPerEnergy: ExchangeRate;
  finalizationCommissionRange: CommissionRange;
  foundationAccountAddress: AccountAddress;
  leverageBound: LeverageFactor;
  microCcdPerEuro: ExchangeRate;
  minimumEquityCapital: Scalars['UnsignedLong'];
  mintPerPayday: Scalars['Decimal'];
  passiveBakingCommission: Scalars['Decimal'];
  passiveFinalizationCommission: Scalars['Decimal'];
  passiveTransactionCommission: Scalars['Decimal'];
  poolOwnerCooldown: Scalars['UnsignedLong'];
  rewardParameters: RewardParametersV2;
  rewardPeriodLength: Scalars['UnsignedLong'];
  transactionCommissionRange: CommissionRange;
};

export type ChainUpdateEnqueued = {
  __typename?: 'ChainUpdateEnqueued';
  effectiveImmediately: Scalars['Boolean'];
  effectiveTime: Scalars['DateTime'];
  payload: ChainUpdatePayload;
};

export type ChainUpdatePayload = AddAnonymityRevokerChainUpdatePayload | AddIdentityProviderChainUpdatePayload | BakerStakeThresholdChainUpdatePayload | BlockEnergyLimitUpdate | CooldownParametersChainUpdatePayload | ElectionDifficultyChainUpdatePayload | EuroPerEnergyChainUpdatePayload | FinalizationCommitteeParametersUpdate | FoundationAccountChainUpdatePayload | GasRewardsChainUpdatePayload | GasRewardsCpv2Update | Level1KeysChainUpdatePayload | MicroCcdPerEuroChainUpdatePayload | MinBlockTimeUpdate | MintDistributionChainUpdatePayload | MintDistributionV1ChainUpdatePayload | PoolParametersChainUpdatePayload | ProtocolChainUpdatePayload | RootKeysChainUpdatePayload | TimeParametersChainUpdatePayload | TimeoutParametersUpdate | TransactionFeeDistributionChainUpdatePayload | ValidatorScoreParametersUpdate;

export type CisBurnEvent = {
  __typename?: 'CisBurnEvent';
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  fromAddress: Address;
  parsed?: Maybe<Scalars['String']>;
  tokenAmount: Scalars['BigInteger'];
  tokenId: Scalars['String'];
  transactionHash: Scalars['String'];
};

export type CisEvent = CisBurnEvent | CisMintEvent | CisTokenMetadataEvent | CisTransferEvent | CisUpdateOperatorEvent;

export type CisMintEvent = {
  __typename?: 'CisMintEvent';
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  parsed?: Maybe<Scalars['String']>;
  toAddress: Address;
  tokenAmount: Scalars['BigInteger'];
  tokenId: Scalars['String'];
  transactionHash: Scalars['String'];
};

export type CisTokenMetadataEvent = {
  __typename?: 'CisTokenMetadataEvent';
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  hashHex?: Maybe<Scalars['String']>;
  metadataUrl: Scalars['String'];
  parsed?: Maybe<Scalars['String']>;
  tokenId: Scalars['String'];
  transactionHash: Scalars['String'];
};

export type CisTransferEvent = {
  __typename?: 'CisTransferEvent';
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  fromAddress: Address;
  parsed?: Maybe<Scalars['String']>;
  toAddress: Address;
  tokenAmount: Scalars['BigInteger'];
  tokenId: Scalars['String'];
  transactionHash: Scalars['String'];
};

export type CisUpdateOperatorEvent = {
  __typename?: 'CisUpdateOperatorEvent';
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  operator: Address;
  owner: Address;
  parsed?: Maybe<Scalars['String']>;
  transactionHash: Scalars['String'];
  update: OperatorUpdateType;
};

/** Information about the offset pagination. */
export type CollectionSegmentInfo = {
  __typename?: 'CollectionSegmentInfo';
  /** Indicates whether more items exist following the set defined by the clients arguments. */
  hasNextPage: Scalars['Boolean'];
  /** Indicates whether more items exist prior the set defined by the clients arguments. */
  hasPreviousPage: Scalars['Boolean'];
};

export type CommissionRange = {
  __typename?: 'CommissionRange';
  max: Scalars['Decimal'];
  min: Scalars['Decimal'];
};

export type CommissionRates = {
  __typename?: 'CommissionRates';
  bakingCommission: Scalars['Decimal'];
  finalizationCommission: Scalars['Decimal'];
  transactionCommission: Scalars['Decimal'];
};

export type Contract = {
  __typename?: 'Contract';
  blockHeight: Scalars['UnsignedLong'];
  blockSlotTime: Scalars['DateTime'];
  contractAddress: Scalars['String'];
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  contractEvents?: Maybe<ContractEventsCollectionSegment>;
  contractRejectEvents?: Maybe<ContractRejectEventsCollectionSegment>;
  creator: AccountAddress;
  snapshot: ContractSnapshot;
  tokens?: Maybe<TokensCollectionSegment>;
  transactionHash: Scalars['String'];
};


export type ContractContractEventsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};


export type ContractContractRejectEventsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};


export type ContractTokensArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};

export type ContractAddress = {
  __typename?: 'ContractAddress';
  asString: Scalars['String'];
  index: Scalars['UnsignedLong'];
  subIndex: Scalars['UnsignedLong'];
};

export type ContractCall = {
  __typename?: 'ContractCall';
  contractUpdated: ContractUpdated;
};

export type ContractEvent = {
  __typename?: 'ContractEvent';
  blockHeight: Scalars['UnsignedLong'];
  blockSlotTime: Scalars['DateTime'];
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  event: Event;
  sender: AccountAddress;
  transactionHash: Scalars['String'];
};

/** A segment of a collection. */
export type ContractEventsCollectionSegment = {
  __typename?: 'ContractEventsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<ContractEvent>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export type ContractInitialized = {
  __typename?: 'ContractInitialized';
  amount: Scalars['UnsignedLong'];
  contractAddress: ContractAddress;
  events?: Maybe<StringConnection>;
  eventsAsHex?: Maybe<StringConnection>;
  initName: Scalars['String'];
  moduleRef: Scalars['String'];
  version?: Maybe<ContractVersion>;
};


export type ContractInitializedEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type ContractInitializedEventsAsHexArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type ContractInterrupted = {
  __typename?: 'ContractInterrupted';
  contractAddress: ContractAddress;
  events?: Maybe<StringConnection>;
  eventsAsHex?: Maybe<StringConnection>;
};


export type ContractInterruptedEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type ContractInterruptedEventsAsHexArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type ContractModuleDeployed = {
  __typename?: 'ContractModuleDeployed';
  moduleRef: Scalars['String'];
};

export type ContractRejectEvent = {
  __typename?: 'ContractRejectEvent';
  blockHeight: Scalars['UnsignedLong'];
  blockSlotTime: Scalars['DateTime'];
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  rejectedEvent: TransactionRejectReason;
  sender: AccountAddress;
  transactionHash: Scalars['String'];
};

/** A segment of a collection. */
export type ContractRejectEventsCollectionSegment = {
  __typename?: 'ContractRejectEventsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<ContractRejectEvent>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export type ContractResumed = {
  __typename?: 'ContractResumed';
  contractAddress: ContractAddress;
  success: Scalars['Boolean'];
};

export type ContractSnapshot = {
  __typename?: 'ContractSnapshot';
  amount: Scalars['UnsignedLong'];
  blockHeight: Scalars['UnsignedLong'];
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  contractName: Scalars['String'];
  moduleReference: Scalars['String'];
};

export type ContractUpdated = {
  __typename?: 'ContractUpdated';
  amount: Scalars['UnsignedLong'];
  contractAddress: ContractAddress;
  events?: Maybe<StringConnection>;
  eventsAsHex?: Maybe<StringConnection>;
  instigator: Address;
  message?: Maybe<Scalars['String']>;
  messageAsHex: Scalars['String'];
  receiveName: Scalars['String'];
  version?: Maybe<ContractVersion>;
};


export type ContractUpdatedEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type ContractUpdatedEventsAsHexArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type ContractUpgraded = {
  __typename?: 'ContractUpgraded';
  contractAddress: ContractAddress;
  from: Scalars['String'];
  to: Scalars['String'];
};

export enum ContractVersion {
  V0 = 'V0',
  V1 = 'V1'
}

/** A connection to a list of items. */
export type ContractsConnection = {
  __typename?: 'ContractsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<ContractsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Contract>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type ContractsEdge = {
  __typename?: 'ContractsEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Contract;
};

export type CooldownParametersChainUpdatePayload = {
  __typename?: 'CooldownParametersChainUpdatePayload';
  delegatorCooldown: Scalars['UnsignedLong'];
  poolOwnerCooldown: Scalars['UnsignedLong'];
};

export type CredentialDeployed = {
  __typename?: 'CredentialDeployed';
  accountAddress: AccountAddress;
  regId: Scalars['String'];
};

export type CredentialDeploymentTransaction = {
  __typename?: 'CredentialDeploymentTransaction';
  credentialDeploymentTransactionType?: Maybe<CredentialDeploymentTransactionType>;
};

/** Enumeration of the types of credentials. */
export enum CredentialDeploymentTransactionType {
  /**
   * Initial credential is a credential that is submitted by the identity
   * provider on behalf of the user. There is at most one initial credential
   * per identity.
   */
  Initial = 'INITIAL',
  /**
   * A normal credential is one where the identity behind it is only known to
   * the owner of the account, unless the identity disclosure process
   * has been initiated.
   */
  Normal = 'NORMAL'
}

export type CredentialHolderDidNotSign = {
  __typename?: 'CredentialHolderDidNotSign';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type CredentialKeysUpdated = {
  __typename?: 'CredentialKeysUpdated';
  credId: Scalars['String'];
};

export type CredentialsUpdated = {
  __typename?: 'CredentialsUpdated';
  accountAddress: AccountAddress;
  newCredIds: Array<Scalars['String']>;
  newThreshold: Scalars['Byte'];
  removedCredIds: Array<Scalars['String']>;
};

export type DataRegistered = {
  __typename?: 'DataRegistered';
  dataAsHex: Scalars['String'];
  decoded: DecodedText;
};

export type DecodedText = {
  __typename?: 'DecodedText';
  decodeType: TextDecodeType;
  text: Scalars['String'];
};

export type Delegation = {
  __typename?: 'Delegation';
  delegationTarget: DelegationTarget;
  delegatorId: Scalars['Long'];
  pendingChange?: Maybe<PendingDelegationChange>;
  restakeEarnings: Scalars['Boolean'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type DelegationAdded = {
  __typename?: 'DelegationAdded';
  accountAddress: AccountAddress;
  delegatorId: Scalars['UnsignedLong'];
};

export type DelegationRemoved = {
  __typename?: 'DelegationRemoved';
  accountAddress: AccountAddress;
  delegatorId: Scalars['UnsignedLong'];
};

export type DelegationSetDelegationTarget = {
  __typename?: 'DelegationSetDelegationTarget';
  accountAddress: AccountAddress;
  delegationTarget: DelegationTarget;
  delegatorId: Scalars['UnsignedLong'];
};

export type DelegationSetRestakeEarnings = {
  __typename?: 'DelegationSetRestakeEarnings';
  accountAddress: AccountAddress;
  delegatorId: Scalars['UnsignedLong'];
  restakeEarnings: Scalars['Boolean'];
};

export type DelegationStakeDecreased = {
  __typename?: 'DelegationStakeDecreased';
  accountAddress: AccountAddress;
  delegatorId: Scalars['UnsignedLong'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type DelegationStakeIncreased = {
  __typename?: 'DelegationStakeIncreased';
  accountAddress: AccountAddress;
  delegatorId: Scalars['UnsignedLong'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type DelegationSummary = {
  __typename?: 'DelegationSummary';
  accountAddress: AccountAddress;
  restakeEarnings: Scalars['Boolean'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type DelegationTarget = BakerDelegationTarget | PassiveDelegationTarget;

export type DelegationTargetNotABaker = {
  __typename?: 'DelegationTargetNotABaker';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  bakerId: Scalars['UnsignedLong'];
};

export type DelegatorInCooldown = {
  __typename?: 'DelegatorInCooldown';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

/** A connection to a list of items. */
export type DelegatorsConnection = {
  __typename?: 'DelegatorsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<DelegatorsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<DelegationSummary>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type DelegatorsEdge = {
  __typename?: 'DelegatorsEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: DelegationSummary;
};

export type DuplicateAggregationKey = {
  __typename?: 'DuplicateAggregationKey';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  aggregationKey: Scalars['String'];
};

export type DuplicateCredIds = {
  __typename?: 'DuplicateCredIds';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  credIds: Array<Scalars['String']>;
};

export type ElectionDifficultyChainUpdatePayload = {
  __typename?: 'ElectionDifficultyChainUpdatePayload';
  electionDifficulty: Scalars['Decimal'];
};

export type EncryptedAmountSelfTransfer = {
  __typename?: 'EncryptedAmountSelfTransfer';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: AccountAddress;
};

export type EncryptedAmountsRemoved = {
  __typename?: 'EncryptedAmountsRemoved';
  accountAddress: AccountAddress;
  inputAmount: Scalars['String'];
  newEncryptedAmount: Scalars['String'];
  upToIndex: Scalars['UnsignedLong'];
};

export type EncryptedSelfAmountAdded = {
  __typename?: 'EncryptedSelfAmountAdded';
  accountAddress: AccountAddress;
  amount: Scalars['UnsignedLong'];
  newEncryptedAmount: Scalars['String'];
};

export type EuroPerEnergyChainUpdatePayload = {
  __typename?: 'EuroPerEnergyChainUpdatePayload';
  exchangeRate: ExchangeRate;
};

export type Event = AccountCreated | AmountAddedByDecryption | BakerAdded | BakerKeysUpdated | BakerRemoved | BakerSetBakingRewardCommission | BakerSetFinalizationRewardCommission | BakerSetMetadataUrl | BakerSetOpenStatus | BakerSetRestakeEarnings | BakerSetTransactionFeeCommission | BakerStakeDecreased | BakerStakeIncreased | ChainUpdateEnqueued | ContractCall | ContractInitialized | ContractInterrupted | ContractModuleDeployed | ContractResumed | ContractUpdated | ContractUpgraded | CredentialDeployed | CredentialKeysUpdated | CredentialsUpdated | DataRegistered | DelegationAdded | DelegationRemoved | DelegationSetDelegationTarget | DelegationSetRestakeEarnings | DelegationStakeDecreased | DelegationStakeIncreased | EncryptedAmountsRemoved | EncryptedSelfAmountAdded | NewEncryptedAmount | TransferMemo | Transferred | TransferredWithSchedule;

/** A connection to a list of items. */
export type EventsConnection = {
  __typename?: 'EventsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<EventsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Event>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
  /** Identifies the total count of items in the connection. */
  totalCount: Scalars['Int'];
};

/** An edge in a connection. */
export type EventsEdge = {
  __typename?: 'EventsEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Event;
};

export type ExchangeRate = {
  __typename?: 'ExchangeRate';
  denominator: Scalars['UnsignedLong'];
  numerator: Scalars['UnsignedLong'];
};

export type FinalizationCommitteeParametersUpdate = {
  __typename?: 'FinalizationCommitteeParametersUpdate';
  finalizersRelativeStakeThreshold: Scalars['Decimal'];
  maxFinalizers: Scalars['UnsignedInt'];
  minFinalizers: Scalars['UnsignedInt'];
};

export type FinalizationRewardCommissionNotInRange = {
  __typename?: 'FinalizationRewardCommissionNotInRange';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type FinalizationRewardsSpecialEvent = {
  __typename?: 'FinalizationRewardsSpecialEvent';
  finalizationRewards?: Maybe<AccountAddressAmountConnection>;
  id: Scalars['ID'];
  remainder: Scalars['UnsignedLong'];
};


export type FinalizationRewardsSpecialEventFinalizationRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type FirstScheduledReleaseExpired = {
  __typename?: 'FirstScheduledReleaseExpired';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type FoundationAccountChainUpdatePayload = {
  __typename?: 'FoundationAccountChainUpdatePayload';
  accountAddress: AccountAddress;
};

export type GasRewards = {
  __typename?: 'GasRewards';
  accountCreation: Scalars['Decimal'];
  baker: Scalars['Decimal'];
  chainUpdate: Scalars['Decimal'];
  finalizationProof: Scalars['Decimal'];
};

export type GasRewardsChainUpdatePayload = {
  __typename?: 'GasRewardsChainUpdatePayload';
  accountCreation: Scalars['Decimal'];
  baker: Scalars['Decimal'];
  chainUpdate: Scalars['Decimal'];
  finalizationProof: Scalars['Decimal'];
};

export type GasRewardsCpv2 = {
  __typename?: 'GasRewardsCpv2';
  accountCreation: Scalars['Decimal'];
  baker: Scalars['Decimal'];
  chainUpdate: Scalars['Decimal'];
};

export type GasRewardsCpv2Update = {
  __typename?: 'GasRewardsCpv2Update';
  accountCreation: Scalars['Decimal'];
  baker: Scalars['Decimal'];
  chainUpdate: Scalars['Decimal'];
};

export type ImportState = {
  __typename?: 'ImportState';
  cumulativeAccountsCreated: Scalars['Long'];
  cumulativeTransactionCount: Scalars['Long'];
  epochDuration: Scalars['Int'];
  genesisBlockHash: Scalars['String'];
  lastBlockSlotTime: Scalars['DateTime'];
  lastGenesisIndex: Scalars['Int'];
  maxBlockHeightWithUpdatedFinalizationTime: Scalars['Long'];
  maxImportedBlockHeight: Scalars['Long'];
  migrationToBakerPoolsCompleted: Scalars['Boolean'];
  nextPendingBakerChangeTime?: Maybe<Scalars['DateTime']>;
  passiveDelegationAdded: Scalars['Boolean'];
  totalBakerCount: Scalars['Int'];
};

export type InsufficientBalanceForBakerStake = {
  __typename?: 'InsufficientBalanceForBakerStake';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InsufficientBalanceForDelegationStake = {
  __typename?: 'InsufficientBalanceForDelegationStake';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InsufficientDelegationStake = {
  __typename?: 'InsufficientDelegationStake';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidAccountReference = {
  __typename?: 'InvalidAccountReference';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: AccountAddress;
};

export type InvalidAccountThreshold = {
  __typename?: 'InvalidAccountThreshold';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidContractAddress = {
  __typename?: 'InvalidContractAddress';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  contractAddress: ContractAddress;
};

export type InvalidCredentialKeySignThreshold = {
  __typename?: 'InvalidCredentialKeySignThreshold';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidCredentials = {
  __typename?: 'InvalidCredentials';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidEncryptedAmountTransferProof = {
  __typename?: 'InvalidEncryptedAmountTransferProof';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidIndexOnEncryptedTransfer = {
  __typename?: 'InvalidIndexOnEncryptedTransfer';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidInitMethod = {
  __typename?: 'InvalidInitMethod';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  initName: Scalars['String'];
  moduleRef: Scalars['String'];
};

export type InvalidModuleReference = {
  __typename?: 'InvalidModuleReference';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  moduleRef: Scalars['String'];
};

export type InvalidProof = {
  __typename?: 'InvalidProof';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidReceiveMethod = {
  __typename?: 'InvalidReceiveMethod';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  moduleRef: Scalars['String'];
  receiveName: Scalars['String'];
};

export type InvalidTransferToPublicProof = {
  __typename?: 'InvalidTransferToPublicProof';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type KeyIndexAlreadyInUse = {
  __typename?: 'KeyIndexAlreadyInUse';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type Level1KeysChainUpdatePayload = {
  __typename?: 'Level1KeysChainUpdatePayload';
  /** @deprecated Don't use! This field is only in the schema since graphql does not allow types without any fields */
  _: Scalars['Boolean'];
};

export type LeverageFactor = {
  __typename?: 'LeverageFactor';
  denominator: Scalars['UnsignedLong'];
  numerator: Scalars['UnsignedLong'];
};

export type LinkedContract = {
  __typename?: 'LinkedContract';
  contractAddress: ContractAddress;
  linkedDateTime: Scalars['DateTime'];
};

/** A segment of a collection. */
export type LinkedContractsCollectionSegment = {
  __typename?: 'LinkedContractsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<LinkedContract>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export enum MetricsPeriod {
  Last7Days = 'LAST7_DAYS',
  Last24Hours = 'LAST24_HOURS',
  Last30Days = 'LAST30_DAYS',
  LastHour = 'LAST_HOUR',
  LastYear = 'LAST_YEAR'
}

export type MicroCcdPerEuroChainUpdatePayload = {
  __typename?: 'MicroCcdPerEuroChainUpdatePayload';
  exchangeRate: ExchangeRate;
};

export type MinBlockTimeUpdate = {
  __typename?: 'MinBlockTimeUpdate';
  durationSeconds: Scalars['UnsignedLong'];
};

export type MintDistributionChainUpdatePayload = {
  __typename?: 'MintDistributionChainUpdatePayload';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
  mintPerSlot: Scalars['Decimal'];
};

export type MintDistributionV0 = {
  __typename?: 'MintDistributionV0';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
  mintPerSlot: Scalars['Decimal'];
};

export type MintDistributionV1 = {
  __typename?: 'MintDistributionV1';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
};

export type MintDistributionV1ChainUpdatePayload = {
  __typename?: 'MintDistributionV1ChainUpdatePayload';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
};

export type MintSpecialEvent = {
  __typename?: 'MintSpecialEvent';
  bakingReward: Scalars['UnsignedLong'];
  finalizationReward: Scalars['UnsignedLong'];
  foundationAccountAddress: AccountAddress;
  id: Scalars['ID'];
  platformDevelopmentCharge: Scalars['UnsignedLong'];
};

export type MissingBakerAddParameters = {
  __typename?: 'MissingBakerAddParameters';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type MissingDelegationAddParameters = {
  __typename?: 'MissingDelegationAddParameters';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ModuleHashAlreadyExists = {
  __typename?: 'ModuleHashAlreadyExists';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  moduleRef: Scalars['String'];
};

export type ModuleNotWf = {
  __typename?: 'ModuleNotWf';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export enum ModuleReferenceContractLinkAction {
  Added = 'ADDED',
  Removed = 'REMOVED'
}

export type ModuleReferenceContractLinkEvent = {
  __typename?: 'ModuleReferenceContractLinkEvent';
  blockHeight: Scalars['UnsignedLong'];
  blockSlotTime: Scalars['DateTime'];
  contractAddress: ContractAddress;
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  linkAction: ModuleReferenceContractLinkAction;
  moduleReference: Scalars['String'];
  sender: AccountAddress;
  transactionHash: Scalars['String'];
};

/** A segment of a collection. */
export type ModuleReferenceContractLinkEventsCollectionSegment = {
  __typename?: 'ModuleReferenceContractLinkEventsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<ModuleReferenceContractLinkEvent>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export type ModuleReferenceEvent = {
  __typename?: 'ModuleReferenceEvent';
  blockHeight: Scalars['UnsignedLong'];
  blockSlotTime: Scalars['DateTime'];
  displaySchema?: Maybe<Scalars['String']>;
  linkedContracts?: Maybe<LinkedContractsCollectionSegment>;
  moduleReference: Scalars['String'];
  moduleReferenceContractLinkEvents?: Maybe<ModuleReferenceContractLinkEventsCollectionSegment>;
  moduleReferenceRejectEvents?: Maybe<ModuleReferenceRejectEventsCollectionSegment>;
  sender: AccountAddress;
  transactionHash: Scalars['String'];
};


export type ModuleReferenceEventLinkedContractsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};


export type ModuleReferenceEventModuleReferenceContractLinkEventsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};


export type ModuleReferenceEventModuleReferenceRejectEventsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};

export type ModuleReferenceRejectEvent = {
  __typename?: 'ModuleReferenceRejectEvent';
  blockHeight: Scalars['UnsignedLong'];
  blockSlotTime: Scalars['DateTime'];
  moduleReference: Scalars['String'];
  rejectedEvent: TransactionRejectReason;
  sender: AccountAddress;
  transactionHash: Scalars['String'];
};

/** A segment of a collection. */
export type ModuleReferenceRejectEventsCollectionSegment = {
  __typename?: 'ModuleReferenceRejectEventsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<ModuleReferenceRejectEvent>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

/** A connection to a list of items. */
export type ModulesConnection = {
  __typename?: 'ModulesConnection';
  /** A list of edges. */
  edges?: Maybe<Array<ModulesEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<ModuleReferenceEvent>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type ModulesEdge = {
  __typename?: 'ModulesEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: ModuleReferenceEvent;
};

export type NewEncryptedAmount = {
  __typename?: 'NewEncryptedAmount';
  accountAddress: AccountAddress;
  encryptedAmount: Scalars['String'];
  newIndex: Scalars['UnsignedLong'];
};

export enum NodeSortDirection {
  Asc = 'ASC',
  Dsc = 'DSC'
}

export enum NodeSortField {
  AveragePing = 'AVERAGE_PING',
  BlocksReceivedCount = 'BLOCKS_RECEIVED_COUNT',
  ClientVersion = 'CLIENT_VERSION',
  ConsensusBakerId = 'CONSENSUS_BAKER_ID',
  FinalizedBlockHeight = 'FINALIZED_BLOCK_HEIGHT',
  NodeName = 'NODE_NAME',
  PeersCount = 'PEERS_COUNT',
  Uptime = 'UPTIME'
}

export type NodeStatus = {
  __typename?: 'NodeStatus';
  averageBytesPerSecondIn: Scalars['Float'];
  averageBytesPerSecondOut: Scalars['Float'];
  averagePing?: Maybe<Scalars['Float']>;
  bakingCommitteeMember: Scalars['String'];
  bestArrivedTime?: Maybe<Scalars['DateTime']>;
  bestBlock: Scalars['String'];
  bestBlockBakerId?: Maybe<Scalars['UnsignedLong']>;
  bestBlockCentralBankAmount?: Maybe<Scalars['UnsignedLong']>;
  bestBlockExecutionCost?: Maybe<Scalars['UnsignedLong']>;
  bestBlockHeight: Scalars['UnsignedLong'];
  bestBlockTotalAmount?: Maybe<Scalars['UnsignedLong']>;
  bestBlockTotalEncryptedAmount?: Maybe<Scalars['UnsignedLong']>;
  bestBlockTransactionCount?: Maybe<Scalars['UnsignedLong']>;
  bestBlockTransactionEnergyCost?: Maybe<Scalars['UnsignedLong']>;
  bestBlockTransactionsSize?: Maybe<Scalars['UnsignedLong']>;
  blockArriveLatencyEma?: Maybe<Scalars['Float']>;
  blockArriveLatencyEmsd?: Maybe<Scalars['Float']>;
  blockArrivePeriodEma?: Maybe<Scalars['Float']>;
  blockArrivePeriodEmsd?: Maybe<Scalars['Float']>;
  blockReceiveLatencyEma?: Maybe<Scalars['Float']>;
  blockReceiveLatencyEmsd?: Maybe<Scalars['Float']>;
  blockReceivePeriodEma?: Maybe<Scalars['Float']>;
  blockReceivePeriodEmsd?: Maybe<Scalars['Float']>;
  blocksReceivedCount?: Maybe<Scalars['UnsignedLong']>;
  blocksVerifiedCount?: Maybe<Scalars['UnsignedLong']>;
  clientVersion: Scalars['String'];
  consensusBakerId?: Maybe<Scalars['UnsignedLong']>;
  consensusRunning: Scalars['Boolean'];
  finalizationCommitteeMember: Scalars['Boolean'];
  finalizationCount?: Maybe<Scalars['UnsignedLong']>;
  finalizationPeriodEma?: Maybe<Scalars['Float']>;
  finalizationPeriodEmsd?: Maybe<Scalars['Float']>;
  finalizedBlock: Scalars['String'];
  finalizedBlockHeight: Scalars['UnsignedLong'];
  finalizedBlockParent: Scalars['String'];
  finalizedTime?: Maybe<Scalars['DateTime']>;
  genesisBlock: Scalars['String'];
  id: Scalars['ID'];
  nodeId: Scalars['String'];
  nodeName?: Maybe<Scalars['String']>;
  packetsReceived: Scalars['UnsignedLong'];
  packetsSent: Scalars['UnsignedLong'];
  peerType: Scalars['String'];
  peersCount: Scalars['UnsignedLong'];
  peersList: Array<PeerReference>;
  transactionsPerBlockEma?: Maybe<Scalars['Float']>;
  transactionsPerBlockEmsd?: Maybe<Scalars['Float']>;
  uptime: Scalars['UnsignedLong'];
};

/** A connection to a list of items. */
export type NodeStatusesConnection = {
  __typename?: 'NodeStatusesConnection';
  /** A list of edges. */
  edges?: Maybe<Array<NodeStatusesEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<NodeStatus>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type NodeStatusesEdge = {
  __typename?: 'NodeStatusesEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: NodeStatus;
};

export type NonExistentCredIds = {
  __typename?: 'NonExistentCredIds';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  credIds: Array<Scalars['String']>;
};

export type NonExistentCredentialId = {
  __typename?: 'NonExistentCredentialId';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NonExistentRewardAccount = {
  __typename?: 'NonExistentRewardAccount';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: AccountAddress;
};

export type NonIncreasingSchedule = {
  __typename?: 'NonIncreasingSchedule';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NotABaker = {
  __typename?: 'NotABaker';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: AccountAddress;
};

export type NotADelegator = {
  __typename?: 'NotADelegator';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: AccountAddress;
};

export type NotAllowedMultipleCredentials = {
  __typename?: 'NotAllowedMultipleCredentials';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NotAllowedToHandleEncrypted = {
  __typename?: 'NotAllowedToHandleEncrypted';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NotAllowedToReceiveEncrypted = {
  __typename?: 'NotAllowedToReceiveEncrypted';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export enum OperatorUpdateType {
  AddOperator = 'ADD_OPERATOR',
  RemoveOperator = 'REMOVE_OPERATOR'
}

export type OutOfEnergy = {
  __typename?: 'OutOfEnergy';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

/** Information about pagination in a connection. */
export type PageInfo = {
  __typename?: 'PageInfo';
  /** When paginating forwards, the cursor to continue. */
  endCursor?: Maybe<Scalars['String']>;
  /** Indicates whether more edges exist following the set defined by the clients arguments. */
  hasNextPage: Scalars['Boolean'];
  /** Indicates whether more edges exist prior the set defined by the clients arguments. */
  hasPreviousPage: Scalars['Boolean'];
  /** When paginating backwards, the cursor to continue. */
  startCursor?: Maybe<Scalars['String']>;
};

export type PassiveDelegation = {
  __typename?: 'PassiveDelegation';
  apy?: Maybe<Scalars['Float']>;
  commissionRates: CommissionRates;
  /** The total amount staked by delegators to passive delegation. */
  delegatedStake: Scalars['UnsignedLong'];
  /** Total stake passively delegated as a percentage of all CCDs in existence. */
  delegatedStakePercentage: Scalars['Decimal'];
  delegatorCount: Scalars['Int'];
  delegators?: Maybe<DelegatorsConnection>;
  poolRewards?: Maybe<PaydayPoolRewardConnection>;
};


export type PassiveDelegationApyArgs = {
  period: ApyPeriod;
};


export type PassiveDelegationDelegatorsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type PassiveDelegationPoolRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type PassiveDelegationPoolRewardTarget = {
  __typename?: 'PassiveDelegationPoolRewardTarget';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type PassiveDelegationTarget = {
  __typename?: 'PassiveDelegationTarget';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type PaydayAccountRewardSpecialEvent = {
  __typename?: 'PaydayAccountRewardSpecialEvent';
  /** The account that got rewarded. */
  account: AccountAddress;
  /** The baking reward at payday to the account. */
  bakerReward: Scalars['UnsignedLong'];
  /** The finalization reward at payday to the account. */
  finalizationReward: Scalars['UnsignedLong'];
  id: Scalars['ID'];
  /** The transaction fee reward at payday to the account. */
  transactionFees: Scalars['UnsignedLong'];
};

export type PaydayFoundationRewardSpecialEvent = {
  __typename?: 'PaydayFoundationRewardSpecialEvent';
  developmentCharge: Scalars['UnsignedLong'];
  foundationAccount: AccountAddress;
  id: Scalars['ID'];
};

export type PaydayPoolReward = {
  __typename?: 'PaydayPoolReward';
  /** The APY calculated for this single reward taking into consideration only the bakers reward and stake. Will be null if there was no baker stake (passive delegation). */
  bakerApy?: Maybe<Scalars['Float']>;
  bakerReward: PaydayPoolRewardAmounts;
  block: Block;
  /** The APY calculated for this single reward taking into consideration only the delegators reward and stake. Will be null if there was no delegated stake. */
  delegatorsApy?: Maybe<Scalars['Float']>;
  finalizationReward: PaydayPoolRewardAmounts;
  id: Scalars['ID'];
  /** The sum of the transaction fees, baker rewards and finalization rewards. */
  sum: PaydayPoolRewardAmounts;
  timestamp: Scalars['DateTime'];
  /** The APY calculated for this single reward taking into consideration the combined reward and stake of baker and delegators. */
  totalApy?: Maybe<Scalars['Float']>;
  transactionFees: PaydayPoolRewardAmounts;
};

export type PaydayPoolRewardAmounts = {
  __typename?: 'PaydayPoolRewardAmounts';
  /** The bakers share of the reward */
  bakerAmount: Scalars['UnsignedLong'];
  /** The delegators share of the reward */
  delegatorsAmount: Scalars['UnsignedLong'];
  /** The total amount (baker + delegators) */
  totalAmount: Scalars['UnsignedLong'];
};

/** A connection to a list of items. */
export type PaydayPoolRewardConnection = {
  __typename?: 'PaydayPoolRewardConnection';
  /** A list of edges. */
  edges?: Maybe<Array<PaydayPoolRewardEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<PaydayPoolReward>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PaydayPoolRewardEdge = {
  __typename?: 'PaydayPoolRewardEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: PaydayPoolReward;
};

export type PaydayPoolRewardSpecialEvent = {
  __typename?: 'PaydayPoolRewardSpecialEvent';
  /** Accrued baking rewards for pool. */
  bakerReward: Scalars['UnsignedLong'];
  /** Accrued finalization rewards for pool. */
  finalizationReward: Scalars['UnsignedLong'];
  id: Scalars['ID'];
  /** The pool awarded. */
  pool: PoolRewardTarget;
  /** Accrued transaction fees for pool. */
  transactionFees: Scalars['UnsignedLong'];
};

export type PaydayStatus = {
  __typename?: 'PaydayStatus';
  nextPaydayTime: Scalars['DateTime'];
  paydaySummaries?: Maybe<PaydaySummariesConnection>;
};


export type PaydayStatusPaydaySummariesArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

/** A connection to a list of items. */
export type PaydaySummariesConnection = {
  __typename?: 'PaydaySummariesConnection';
  /** A list of edges. */
  edges?: Maybe<Array<PaydaySummariesEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<PaydaySummary>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PaydaySummariesEdge = {
  __typename?: 'PaydaySummariesEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: PaydaySummary;
};

export type PaydaySummary = {
  __typename?: 'PaydaySummary';
  block: Block;
};

export type PeerReference = {
  __typename?: 'PeerReference';
  nodeId: Scalars['String'];
  /** The node status of the peer. Will be null if no status for the peer exists. */
  nodeStatus?: Maybe<NodeStatus>;
};

export type PendingBakerChange = PendingBakerReduceStake | PendingBakerRemoval;

export type PendingBakerReduceStake = {
  __typename?: 'PendingBakerReduceStake';
  effectiveTime: Scalars['DateTime'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type PendingBakerRemoval = {
  __typename?: 'PendingBakerRemoval';
  effectiveTime: Scalars['DateTime'];
};

export type PendingDelegationChange = PendingDelegationReduceStake | PendingDelegationRemoval;

export type PendingDelegationReduceStake = {
  __typename?: 'PendingDelegationReduceStake';
  effectiveTime: Scalars['DateTime'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type PendingDelegationRemoval = {
  __typename?: 'PendingDelegationRemoval';
  effectiveTime: Scalars['DateTime'];
};

export type PoolApy = {
  __typename?: 'PoolApy';
  bakerApy?: Maybe<Scalars['Float']>;
  delegatorsApy?: Maybe<Scalars['Float']>;
  totalApy?: Maybe<Scalars['Float']>;
};

export type PoolClosed = {
  __typename?: 'PoolClosed';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type PoolParametersChainUpdatePayload = {
  __typename?: 'PoolParametersChainUpdatePayload';
  bakingCommissionRange: CommissionRange;
  capitalBound: Scalars['Decimal'];
  finalizationCommissionRange: CommissionRange;
  leverageBound: LeverageFactor;
  minimumEquityCapital: Scalars['UnsignedLong'];
  passiveBakingCommission: Scalars['Decimal'];
  passiveFinalizationCommission: Scalars['Decimal'];
  passiveTransactionCommission: Scalars['Decimal'];
  transactionCommissionRange: CommissionRange;
};

export type PoolRewardMetrics = {
  __typename?: 'PoolRewardMetrics';
  buckets: PoolRewardMetricsBuckets;
  /** Sum of all rewards in requested period that were awarded to the baker (as micro CCD) */
  sumBakerRewardAmount: Scalars['Long'];
  /** Sum of all rewards in requested period that were awarded to the delegators (as micro CCD) */
  sumDelegatorsRewardAmount: Scalars['Long'];
  /** Sum of all rewards in requested period as micro CCD */
  sumTotalRewardAmount: Scalars['Long'];
};

export type PoolRewardMetricsBuckets = {
  __typename?: 'PoolRewardMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** Sum of rewards that were awarded to the baker (as micro CCD) within bucket time period. Intended y-axis value. */
  y_SumBakerRewards: Array<Scalars['Long']>;
  /** Sum of rewards that were awarded to the delegators (as micro CCD) within bucket time period. Intended y-axis value. */
  y_SumDelegatorsRewards: Array<Scalars['Long']>;
  /** Sum of rewards (as micro CCD) within bucket time period. Intended y-axis value. */
  y_SumTotalRewards: Array<Scalars['Long']>;
};

export type PoolRewardTarget = BakerPoolRewardTarget | PassiveDelegationPoolRewardTarget;

export type PoolWouldBecomeOverDelegated = {
  __typename?: 'PoolWouldBecomeOverDelegated';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ProtocolChainUpdatePayload = {
  __typename?: 'ProtocolChainUpdatePayload';
  message: Scalars['String'];
  specificationAuxiliaryDataAsHex: Scalars['String'];
  specificationHash: Scalars['String'];
  specificationUrl: Scalars['String'];
};

export type Query = {
  __typename?: 'Query';
  account?: Maybe<Account>;
  accountByAddress?: Maybe<Account>;
  accounts?: Maybe<AccountsConnection>;
  accountsMetrics?: Maybe<AccountsMetrics>;
  baker?: Maybe<Baker>;
  bakerByBakerId?: Maybe<Baker>;
  bakerMetrics: BakerMetrics;
  bakers?: Maybe<BakersConnection>;
  block?: Maybe<Block>;
  blockByBlockHash?: Maybe<Block>;
  blockMetrics: BlockMetrics;
  blocks?: Maybe<BlocksConnection>;
  contract?: Maybe<Contract>;
  contracts?: Maybe<ContractsConnection>;
  importState?: Maybe<ImportState>;
  latestChainParameters?: Maybe<ChainParameters>;
  moduleReferenceEvent?: Maybe<ModuleReferenceEvent>;
  nodeStatus?: Maybe<NodeStatus>;
  nodeStatuses?: Maybe<NodeStatusesConnection>;
  passiveDelegation?: Maybe<PassiveDelegation>;
  paydayStatus?: Maybe<PaydayStatus>;
  poolRewardMetricsForBakerPool: PoolRewardMetrics;
  poolRewardMetricsForPassiveDelegation: PoolRewardMetrics;
  rewardMetrics: RewardMetrics;
  rewardMetricsForAccount: RewardMetrics;
  search: SearchResult;
  suspendedValidators: SuspendedValidators;
  token: Token;
  tokens?: Maybe<TokensConnection>;
  transaction?: Maybe<Transaction>;
  transactionByTransactionHash?: Maybe<Transaction>;
  transactionMetrics?: Maybe<TransactionMetrics>;
  transactions?: Maybe<TransactionsConnection>;
  versions: Versions;
};

export type SuspendedValidators = {
  __typename?: 'SuspendedValidators';
  primedForSuspensionValidators: ValidatorsConnection;
  suspendedValidators: ValidatorsConnection;
};


export type SuspendedValidatorsPrimedForSuspensionValidatorsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SuspendedValidatorsSuspendedValidatorsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type Validators = {
  __typename?: 'Validators';
  id: Scalars['Int'];
};

export type ValidatorsConnection = {
  __typename?: 'ValidatorsConnection';
  /** A list of edges. */
  edges: Array<ValidatorsEdge>;
  /** A list of nodes. */
  nodes: Array<Validators>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type ValidatorsEdge = {
  __typename?: 'ValidatorsEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Validators;
};

export type QueryAccountArgs = {
  id: Scalars['ID'];
};


export type QueryAccountByAddressArgs = {
  accountAddress: Scalars['String'];
};


export type QueryAccountsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  filter?: InputMaybe<AccountFilterInput>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
  sort?: AccountSort;
};


export type QueryAccountsMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryBakerArgs = {
  id: Scalars['ID'];
};


export type QueryBakerByBakerIdArgs = {
  bakerId: Scalars['Long'];
};


export type QueryBakerMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryBakersArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  filter?: InputMaybe<BakerFilterInput>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
  sort?: BakerSort;
};


export type QueryBlockArgs = {
  id: Scalars['ID'];
};


export type QueryBlockByBlockHashArgs = {
  blockHash: Scalars['String'];
};


export type QueryBlockMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryBlocksArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type QueryContractArgs = {
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
};


export type QueryContractsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type QueryModuleReferenceEventArgs = {
  moduleReference: Scalars['String'];
};


export type QueryNodeStatusArgs = {
  id: Scalars['ID'];
};


export type QueryNodeStatusesArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
  sortDirection: NodeSortDirection;
  sortField: NodeSortField;
};


export type QueryPoolRewardMetricsForBakerPoolArgs = {
  bakerId: Scalars['ID'];
  period: MetricsPeriod;
};


export type QueryPoolRewardMetricsForPassiveDelegationArgs = {
  period: MetricsPeriod;
};


export type QueryRewardMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryRewardMetricsForAccountArgs = {
  accountId: Scalars['ID'];
  period: MetricsPeriod;
};


export type QuerySearchArgs = {
  query: Scalars['String'];
};


export type QueryTokenArgs = {
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  tokenId: Scalars['String'];
};


export type QueryTokensArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type QueryTransactionArgs = {
  id: Scalars['ID'];
};


export type QueryTransactionByTransactionHashArgs = {
  transactionHash: Scalars['String'];
};


export type QueryTransactionMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryTransactionsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type Ranking = {
  __typename?: 'Ranking';
  rank: Scalars['Int'];
  total: Scalars['Int'];
};

export type Ratio = {
  __typename?: 'Ratio';
  denominator: Scalars['UnsignedLong'];
  numerator: Scalars['UnsignedLong'];
};

export type Rejected = {
  __typename?: 'Rejected';
  reason: TransactionRejectReason;
};

export type RejectedInit = {
  __typename?: 'RejectedInit';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  rejectReason: Scalars['Int'];
};

export type RejectedReceive = {
  __typename?: 'RejectedReceive';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  contractAddress: ContractAddress;
  message?: Maybe<Scalars['String']>;
  messageAsHex: Scalars['String'];
  receiveName: Scalars['String'];
  rejectReason: Scalars['Int'];
};

export type RemoveFirstCredential = {
  __typename?: 'RemoveFirstCredential';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type RemovedBakerState = {
  __typename?: 'RemovedBakerState';
  removedAt: Scalars['DateTime'];
};

export type RewardMetrics = {
  __typename?: 'RewardMetrics';
  buckets: RewardMetricsBuckets;
  /** Sum of all rewards in requested period as micro CCD */
  sumRewardAmount: Scalars['Long'];
};

export type RewardMetricsBuckets = {
  __typename?: 'RewardMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** Sum of rewards as micro CCD within bucket time period. Intended y-axis value. */
  y_SumRewards: Array<Scalars['Long']>;
};

export type RewardParametersV0 = {
  __typename?: 'RewardParametersV0';
  gasRewards: GasRewards;
  mintDistribution: MintDistributionV0;
  transactionFeeDistribution: TransactionFeeDistribution;
};

export type RewardParametersV1 = {
  __typename?: 'RewardParametersV1';
  gasRewards: GasRewards;
  mintDistribution: MintDistributionV1;
  transactionFeeDistribution: TransactionFeeDistribution;
};

export type RewardParametersV2 = {
  __typename?: 'RewardParametersV2';
  gasRewards: GasRewardsCpv2;
  mintDistribution: MintDistributionV1;
  transactionFeeDistribution: TransactionFeeDistribution;
};

export enum RewardType {
  BakerReward = 'BAKER_REWARD',
  FinalizationReward = 'FINALIZATION_REWARD',
  FoundationReward = 'FOUNDATION_REWARD',
  TransactionFeeReward = 'TRANSACTION_FEE_REWARD'
}

export type RootKeysChainUpdatePayload = {
  __typename?: 'RootKeysChainUpdatePayload';
  /** @deprecated Don't use! This field is only in the schema since graphql does not allow types without any fields */
  _: Scalars['Boolean'];
};

export type RuntimeFailure = {
  __typename?: 'RuntimeFailure';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ScheduledSelfTransfer = {
  __typename?: 'ScheduledSelfTransfer';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: AccountAddress;
};

export type SearchResult = {
  __typename?: 'SearchResult';
  accounts?: Maybe<AccountsConnection>;
  bakers?: Maybe<BakersConnection>;
  blocks?: Maybe<BlocksConnection>;
  contracts?: Maybe<ContractsConnection>;
  modules?: Maybe<ModulesConnection>;
  nodeStatuses?: Maybe<NodeStatusesConnection>;
  tokens?: Maybe<TokensConnection>;
  transactions?: Maybe<TransactionsConnection>;
};


export type SearchResultAccountsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultBakersArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultBlocksArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultContractsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultModulesArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultNodeStatusesArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultTokensArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type SearchResultTransactionsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type SerializationFailure = {
  __typename?: 'SerializationFailure';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type SpecialEvent = BakingRewardsSpecialEvent | BlockAccrueRewardSpecialEvent | BlockRewardsSpecialEvent | FinalizationRewardsSpecialEvent | MintSpecialEvent | PaydayAccountRewardSpecialEvent | PaydayFoundationRewardSpecialEvent | PaydayPoolRewardSpecialEvent;

export enum SpecialEventTypeFilter {
  BakingRewards = 'BAKING_REWARDS',
  BlockAccrueReward = 'BLOCK_ACCRUE_REWARD',
  BlockRewards = 'BLOCK_REWARDS',
  FinalizationRewards = 'FINALIZATION_REWARDS',
  Mint = 'MINT',
  PaydayAccountReward = 'PAYDAY_ACCOUNT_REWARD',
  PaydayFoundationReward = 'PAYDAY_FOUNDATION_REWARD',
  PaydayPoolReward = 'PAYDAY_POOL_REWARD'
}

/** A connection to a list of items. */
export type SpecialEventsConnection = {
  __typename?: 'SpecialEventsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<SpecialEventsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<SpecialEvent>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type SpecialEventsEdge = {
  __typename?: 'SpecialEventsEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: SpecialEvent;
};

export type StakeOverMaximumThresholdForPool = {
  __typename?: 'StakeOverMaximumThresholdForPool';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type StakeUnderMinimumThresholdForBaking = {
  __typename?: 'StakeUnderMinimumThresholdForBaking';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

/** A connection to a list of items. */
export type StringConnection = {
  __typename?: 'StringConnection';
  /** A list of edges. */
  edges?: Maybe<Array<StringEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Scalars['String']>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type StringEdge = {
  __typename?: 'StringEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Scalars['String'];
};

export type Subscription = {
  __typename?: 'Subscription';
  accountsUpdated: AccountsUpdatedSubscriptionItem;
  blockAdded: Block;
};


export type SubscriptionAccountsUpdatedArgs = {
  accountAddress: Scalars['String'];
};

export type Success = {
  __typename?: 'Success';
  events?: Maybe<EventsConnection>;
};


export type SuccessEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export enum TextDecodeType {
  Cbor = 'CBOR',
  Hex = 'HEX'
}

export type TimeParametersChainUpdatePayload = {
  __typename?: 'TimeParametersChainUpdatePayload';
  mintPerPayday: Scalars['Decimal'];
  rewardPeriodLength: Scalars['UnsignedLong'];
};

export type TimeoutParametersUpdate = {
  __typename?: 'TimeoutParametersUpdate';
  decrease: Ratio;
  durationSeconds: Scalars['UnsignedLong'];
  increase: Ratio;
};

export type TimestampedAmount = {
  __typename?: 'TimestampedAmount';
  amount: Scalars['UnsignedLong'];
  timestamp: Scalars['DateTime'];
};

export type Token = {
  __typename?: 'Token';
  accounts?: Maybe<AccountsCollectionSegment>;
  contractAddressFormatted: Scalars['String'];
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  initialTransaction: Transaction;
  metadataUrl?: Maybe<Scalars['String']>;
  tokenAddress: Scalars['String'];
  tokenEvents?: Maybe<TokenEventsCollectionSegment>;
  tokenId: Scalars['String'];
  totalSupply: Scalars['BigInteger'];
};


export type TokenAccountsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};


export type TokenTokenEventsArgs = {
  skip?: InputMaybe<Scalars['Int']>;
  take?: InputMaybe<Scalars['Int']>;
};

export type TokenEvent = {
  __typename?: 'TokenEvent';
  contractIndex: Scalars['UnsignedLong'];
  contractSubIndex: Scalars['UnsignedLong'];
  event: CisEvent;
  id: Scalars['ID'];
  tokenId: Scalars['String'];
  transaction?: Maybe<Transaction>;
};

/** A segment of a collection. */
export type TokenEventsCollectionSegment = {
  __typename?: 'TokenEventsCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<TokenEvent>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

/** A segment of a collection. */
export type TokensCollectionSegment = {
  __typename?: 'TokensCollectionSegment';
  /** A flattened list of the items. */
  items?: Maybe<Array<Token>>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

/** A connection to a list of items. */
export type TokensConnection = {
  __typename?: 'TokensConnection';
  /** A list of edges. */
  edges?: Maybe<Array<TokensEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Token>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type TokensEdge = {
  __typename?: 'TokensEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Token;
};

export type Transaction = {
  __typename?: 'Transaction';
  block: Block;
  ccdCost: Scalars['UnsignedLong'];
  energyCost: Scalars['UnsignedLong'];
  id: Scalars['ID'];
  result: TransactionResult;
  senderAccountAddress?: Maybe<AccountAddress>;
  transactionHash: Scalars['String'];
  transactionIndex: Scalars['Int'];
  transactionType: TransactionType;
};

export type TransactionFeeCommissionNotInRange = {
  __typename?: 'TransactionFeeCommissionNotInRange';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type TransactionFeeDistribution = {
  __typename?: 'TransactionFeeDistribution';
  baker: Scalars['Decimal'];
  gasAccount: Scalars['Decimal'];
};

export type TransactionFeeDistributionChainUpdatePayload = {
  __typename?: 'TransactionFeeDistributionChainUpdatePayload';
  baker: Scalars['Decimal'];
  gasAccount: Scalars['Decimal'];
};

export type TransactionMetrics = {
  __typename?: 'TransactionMetrics';
  buckets: TransactionMetricsBuckets;
  /** Total number of transactions (all time) */
  lastCumulativeTransactionCount: Scalars['Long'];
  /** Total number of transactions in requested period. */
  transactionCount: Scalars['Int'];
};

export type TransactionMetricsBuckets = {
  __typename?: 'TransactionMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** Total number of transactions (all time) at the end of the bucket period. Intended y-axis value. */
  y_LastCumulativeTransactionCount: Array<Scalars['Long']>;
  /** Total number of transactions within the bucket time period. Intended y-axis value. */
  y_TransactionCount: Array<Scalars['Int']>;
};

export type TransactionRejectReason = AlreadyABaker | AlreadyADelegator | AmountTooLarge | BakerInCooldown | BakingRewardCommissionNotInRange | CredentialHolderDidNotSign | DelegationTargetNotABaker | DelegatorInCooldown | DuplicateAggregationKey | DuplicateCredIds | EncryptedAmountSelfTransfer | FinalizationRewardCommissionNotInRange | FirstScheduledReleaseExpired | InsufficientBalanceForBakerStake | InsufficientBalanceForDelegationStake | InsufficientDelegationStake | InvalidAccountReference | InvalidAccountThreshold | InvalidContractAddress | InvalidCredentialKeySignThreshold | InvalidCredentials | InvalidEncryptedAmountTransferProof | InvalidIndexOnEncryptedTransfer | InvalidInitMethod | InvalidModuleReference | InvalidProof | InvalidReceiveMethod | InvalidTransferToPublicProof | KeyIndexAlreadyInUse | MissingBakerAddParameters | MissingDelegationAddParameters | ModuleHashAlreadyExists | ModuleNotWf | NonExistentCredIds | NonExistentCredentialId | NonExistentRewardAccount | NonIncreasingSchedule | NotABaker | NotADelegator | NotAllowedMultipleCredentials | NotAllowedToHandleEncrypted | NotAllowedToReceiveEncrypted | OutOfEnergy | PoolClosed | PoolWouldBecomeOverDelegated | RejectedInit | RejectedReceive | RemoveFirstCredential | RuntimeFailure | ScheduledSelfTransfer | SerializationFailure | StakeOverMaximumThresholdForPool | StakeUnderMinimumThresholdForBaking | TransactionFeeCommissionNotInRange | ZeroScheduledAmount;

export type TransactionResult = Rejected | Success;

export type TransactionType = AccountTransaction | CredentialDeploymentTransaction | UpdateTransaction;

/** A connection to a list of items. */
export type TransactionsConnection = {
  __typename?: 'TransactionsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<TransactionsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Transaction>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type TransactionsEdge = {
  __typename?: 'TransactionsEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: Transaction;
};

export type TransferMemo = {
  __typename?: 'TransferMemo';
  decoded: DecodedText;
  rawHex: Scalars['String'];
};

export type Transferred = {
  __typename?: 'Transferred';
  amount: Scalars['UnsignedLong'];
  from: Address;
  to: Address;
};

export type TransferredWithSchedule = {
  __typename?: 'TransferredWithSchedule';
  amountsSchedule?: Maybe<AmountsScheduleConnection>;
  fromAccountAddress: AccountAddress;
  toAccountAddress: AccountAddress;
  totalAmount: Scalars['UnsignedLong'];
};


export type TransferredWithScheduleAmountsScheduleArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type UpdateTransaction = {
  __typename?: 'UpdateTransaction';
  updateTransactionType?: Maybe<UpdateTransactionType>;
};

/** The type of an update. */
export enum UpdateTransactionType {
  /** Update of block energy limit parameters. */
  BlockEnergyLimitUpdate = 'BLOCK_ENERGY_LIMIT_UPDATE',
  /** Update of finalization committee parameters. */
  FinalizationCommitteeParametersUpdate = 'FINALIZATION_COMMITTEE_PARAMETERS_UPDATE',
  /** Update of distribution of GAS rewards. */
  GasRewardsCpv2Update = 'GAS_REWARDS_CPV2_UPDATE',
  /** Update of distribution of minted CCD. */
  MintDistributionCpv1Update = 'MINT_DISTRIBUTION_CPV1_UPDATE',
  /** Update of min-block-time parameters. */
  MinBlockTimeUpdate = 'MIN_BLOCK_TIME_UPDATE',
  /** Update of timeout parameters. */
  TimeoutParametersUpdate = 'TIMEOUT_PARAMETERS_UPDATE',
  /** Introduce new Identity Disclosure Authority. */
  UpdateAddAnonymityRevoker = 'UPDATE_ADD_ANONYMITY_REVOKER',
  /** Introduce new Identity Provider. */
  UpdateAddIdentityProvider = 'UPDATE_ADD_IDENTITY_PROVIDER',
  /** Update of minimum threshold for becoming a validator. */
  UpdateBakerStakeThreshold = 'UPDATE_BAKER_STAKE_THRESHOLD',
  /** Update of cooldown parameters. */
  UpdateCooldownParameters = 'UPDATE_COOLDOWN_PARAMETERS',
  /** Update of the election difficulty. */
  UpdateElectionDifficulty = 'UPDATE_ELECTION_DIFFICULTY',
  /** Update of conversion rate of Euro per energy. */
  UpdateEuroPerEnergy = 'UPDATE_EURO_PER_ENERGY',
  /** Update of account marked as foundation account. */
  UpdateFoundationAccount = 'UPDATE_FOUNDATION_ACCOUNT',
  /** Update of distribution of GAS rewards. */
  UpdateGasRewards = 'UPDATE_GAS_REWARDS',
  /** Update of level1 keys. */
  UpdateLevel1Keys = 'UPDATE_LEVEL1_KEYS',
  /** Update of level2 keys. */
  UpdateLevel2Keys = 'UPDATE_LEVEL2_KEYS',
  /** Update of conversion rate of CCD per Euro. */
  UpdateMicroGtuPerEuro = 'UPDATE_MICRO_GTU_PER_EURO',
  /** Update of distribution of minted CCD. */
  UpdateMintDistribution = 'UPDATE_MINT_DISTRIBUTION',
  /** Update of pool parameters. */
  UpdatePoolParameters = 'UPDATE_POOL_PARAMETERS',
  /** Update of protocol version. */
  UpdateProtocol = 'UPDATE_PROTOCOL',
  /** Update of root keys. */
  UpdateRootKeys = 'UPDATE_ROOT_KEYS',
  /** Update of time parameters. */
  UpdateTimeParameters = 'UPDATE_TIME_PARAMETERS',
  /** Update of distribution of transaction fee. */
  UpdateTransactionFeeDistribution = 'UPDATE_TRANSACTION_FEE_DISTRIBUTION',
  /** Update of validator score parameters. */
  ValidatorScoreParametersUpdate = 'VALIDATOR_SCORE_PARAMETERS_UPDATE'
}

export type ValidatorScoreParametersUpdate = {
  __typename?: 'ValidatorScoreParametersUpdate';
  maximumMissedRounds: Scalars['UnsignedLong'];
};

export type Versions = {
  __typename?: 'Versions';
  backendVersion: Scalars['String'];
};

export type ZeroScheduledAmount = {
  __typename?: 'ZeroScheduledAmount';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};
