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
  Byte: any;
  /**
   * Implement the DateTime<Utc> scalar
   *
   * The input/output is a string in RFC3339 format.
   */
  DateTime: any;
  Decimal: any;
  /** A scalar that can represent any JSON value. */
  JSON: any;
  Long: any;
  TimeSpan: any;
  UnsignedInt: any;
  UnsignedLong: any;
};

export type Account = {
  __typename?: 'Account';
  accountStatement: AccountStatementEntryConnection;
  /** The address of the account in Base58Check. */
  address: AccountAddress;
  /** The total amount of CCD hold by the account. */
  amount: Scalars['UnsignedLong'];
  baker?: Maybe<Baker>;
  /** Timestamp of the block where this account was created. */
  createdAt: Scalars['DateTime'];
  delegation?: Maybe<Delegation>;
  id: Scalars['ID'];
  /**
   * Number of transactions where this account is the sender of the
   * transaction. This is the `nonce` of the account. This value is
   * currently not used by the front-end and the `COUNT(*)` will need further
   * optimization if intended to be used.
   */
  nonce: Scalars['Int'];
  plts: AccountProtocolTokenConnection;
  releaseSchedule: AccountReleaseSchedule;
  rewards: AccountRewardConnection;
  tokens: AccountTokenConnection;
  /**
   * Number of transactions the account has been involved in or
   * affected by.
   */
  transactionCount: Scalars['Int'];
  transactions: AccountTransactionRelationConnection;
};


export type AccountAccountStatementArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type AccountPltsArgs = {
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

export type AccountAddressAmountConnection = {
  __typename?: 'AccountAddressAmountConnection';
  /** A list of edges. */
  edges: Array<AccountAddressAmountEdge>;
  /** A list of nodes. */
  nodes: Array<AccountAddressAmount>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountAddressAmountEdge = {
  __typename?: 'AccountAddressAmountEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: AccountAddressAmount;
};

export type AccountConnection = {
  __typename?: 'AccountConnection';
  /** A list of edges. */
  edges: Array<AccountEdge>;
  /** A list of nodes. */
  nodes: Array<Account>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

export type AccountCreated = {
  __typename?: 'AccountCreated';
  accountAddress: AccountAddress;
};

/** An edge in a connection. */
export type AccountEdge = {
  __typename?: 'AccountEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Account;
};

export type AccountFilterInput = {
  isDelegator: Scalars['Boolean'];
};

export type AccountMetricsBuckets = {
  __typename?: 'AccountMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /**
   * Number of accounts created within bucket time period. Intended y-axis
   * value.
   */
  y_AccountsCreated: Array<Scalars['Int']>;
  /**
   * Total number of accounts created (all time) at the end of the bucket
   * period. Intended y-axis value.
   */
  y_LastCumulativeAccountsCreated: Array<Scalars['Int']>;
};

export type AccountProtocolToken = {
  __typename?: 'AccountProtocolToken';
  amount: Scalars['Int'];
  decimal: Scalars['Int'];
  tokenId: Scalars['String'];
  tokenName: Scalars['String'];
};

export type AccountProtocolTokenConnection = {
  __typename?: 'AccountProtocolTokenConnection';
  /** A list of edges. */
  edges: Array<AccountProtocolTokenEdge>;
  /** A list of nodes. */
  nodes: Array<AccountProtocolToken>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountProtocolTokenEdge = {
  __typename?: 'AccountProtocolTokenEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: AccountProtocolToken;
};

export type AccountReleaseSchedule = {
  __typename?: 'AccountReleaseSchedule';
  schedule: AccountReleaseScheduleItemConnection;
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

export type AccountReleaseScheduleItemConnection = {
  __typename?: 'AccountReleaseScheduleItemConnection';
  /** A list of edges. */
  edges: Array<AccountReleaseScheduleItemEdge>;
  /** A list of nodes. */
  nodes: Array<AccountReleaseScheduleItem>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountReleaseScheduleItemEdge = {
  __typename?: 'AccountReleaseScheduleItemEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
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

export type AccountRewardConnection = {
  __typename?: 'AccountRewardConnection';
  /** A list of edges. */
  edges: Array<AccountRewardEdge>;
  /** A list of nodes. */
  nodes: Array<AccountReward>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountRewardEdge = {
  __typename?: 'AccountRewardEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
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

export type AccountStatementEntryConnection = {
  __typename?: 'AccountStatementEntryConnection';
  /** A list of edges. */
  edges: Array<AccountStatementEntryEdge>;
  /** A list of nodes. */
  nodes: Array<AccountStatementEntry>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountStatementEntryEdge = {
  __typename?: 'AccountStatementEntryEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
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
  accountId: Scalars['Int'];
  balance: Scalars['BigInteger'];
  changeSeq: Scalars['Int'];
  contractIndex: Scalars['Int'];
  contractSubIndex: Scalars['Int'];
  token: Token;
  tokenId: Scalars['String'];
};

export type AccountTokenConnection = {
  __typename?: 'AccountTokenConnection';
  /** A list of edges. */
  edges: Array<AccountTokenEdge>;
  /** A list of nodes. */
  nodes: Array<AccountToken>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountTokenEdge = {
  __typename?: 'AccountTokenEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
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

export type AccountTransactionRelationConnection = {
  __typename?: 'AccountTransactionRelationConnection';
  /** A list of edges. */
  edges: Array<AccountTransactionRelationEdge>;
  /** A list of nodes. */
  nodes: Array<AccountTransactionRelation>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type AccountTransactionRelationEdge = {
  __typename?: 'AccountTransactionRelationEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: AccountTransactionRelation;
};

export enum AccountTransactionType {
  AddBaker = 'ADD_BAKER',
  ConfigureBaker = 'CONFIGURE_BAKER',
  ConfigureDelegation = 'CONFIGURE_DELEGATION',
  DeployModule = 'DEPLOY_MODULE',
  EncryptedTransfer = 'ENCRYPTED_TRANSFER',
  EncryptedTransferWithMemo = 'ENCRYPTED_TRANSFER_WITH_MEMO',
  InitializeSmartContractInstance = 'INITIALIZE_SMART_CONTRACT_INSTANCE',
  RegisterData = 'REGISTER_DATA',
  RemoveBaker = 'REMOVE_BAKER',
  SimpleTransfer = 'SIMPLE_TRANSFER',
  SimpleTransferWithMemo = 'SIMPLE_TRANSFER_WITH_MEMO',
  TokenUpdate = 'TOKEN_UPDATE',
  TransferToEncrypted = 'TRANSFER_TO_ENCRYPTED',
  TransferToPublic = 'TRANSFER_TO_PUBLIC',
  TransferWithSchedule = 'TRANSFER_WITH_SCHEDULE',
  TransferWithScheduleWithMemo = 'TRANSFER_WITH_SCHEDULE_WITH_MEMO',
  UpdateBakerKeys = 'UPDATE_BAKER_KEYS',
  UpdateBakerRestakeEarnings = 'UPDATE_BAKER_RESTAKE_EARNINGS',
  UpdateBakerStake = 'UPDATE_BAKER_STAKE',
  UpdateCredentials = 'UPDATE_CREDENTIALS',
  UpdateCredentialKeys = 'UPDATE_CREDENTIAL_KEYS',
  UpdateSmartContractInstance = 'UPDATE_SMART_CONTRACT_INSTANCE'
}

/** A segment of a collection. */
export type AccountsCollectionSegment = {
  __typename?: 'AccountsCollectionSegment';
  /** A flattened list of the items. */
  items: Array<AccountToken>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export type AccountsMetrics = {
  __typename?: 'AccountsMetrics';
  /** Total number of accounts created in requested period. */
  accountsCreated: Scalars['Int'];
  buckets: AccountMetricsBuckets;
  /** Total number of accounts created (all time). */
  lastCumulativeAccountsCreated: Scalars['Int'];
};

export type AccountsUpdatedSubscriptionItem = {
  __typename?: 'AccountsUpdatedSubscriptionItem';
  address: Scalars['String'];
};

export type ActiveBakerState = {
  __typename?: 'ActiveBakerState';
  /**
   * The status of the baker's node. Will be null if no status for the node
   * exists.
   */
  nodeStatus?: Maybe<NodeStatus>;
  pendingChange?: Maybe<PendingBakerChange>;
  pool: BakerPool;
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
  bakerId: Scalars['Long'];
};

export type AlreadyADelegator = {
  __typename?: 'AlreadyADelegator';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type AmountAddedByDecryption = {
  __typename?: 'AmountAddedByDecryption';
  accountAddress: AccountAddress;
  amount: Scalars['UnsignedLong'];
};

export type AmountTooLarge = {
  __typename?: 'AmountTooLarge';
  address: Address;
  amount: Scalars['UnsignedLong'];
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
  transactions: InterimTransactionConnection;
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
  bakerId: Scalars['Long'];
  electionKey: Scalars['String'];
  restakeEarnings: Scalars['Boolean'];
  signKey: Scalars['String'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type BakerConnection = {
  __typename?: 'BakerConnection';
  /** A list of edges. */
  edges: Array<BakerEdge>;
  /** A list of nodes. */
  nodes: Array<Baker>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

export type BakerDelegationRemoved = {
  __typename?: 'BakerDelegationRemoved';
  accountAddress: AccountAddress;
  delegatorId: Scalars['Int'];
};

export type BakerDelegationTarget = {
  __typename?: 'BakerDelegationTarget';
  bakerId: Scalars['Long'];
};

/** An edge in a connection. */
export type BakerEdge = {
  __typename?: 'BakerEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Baker;
};

export type BakerFilterInput = {
  includeRemoved?: InputMaybe<Scalars['Boolean']>;
  openStatusFilter?: InputMaybe<BakerPoolOpenStatus>;
};

export type BakerInCooldown = {
  __typename?: 'BakerInCooldown';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type BakerKeysUpdated = {
  __typename?: 'BakerKeysUpdated';
  accountAddress: AccountAddress;
  aggregationKey: Scalars['String'];
  bakerId: Scalars['Long'];
  electionKey: Scalars['String'];
  signKey: Scalars['String'];
};

export type BakerMetrics = {
  __typename?: 'BakerMetrics';
  /** The number of bakers added during the specified period. */
  bakersAdded: Scalars['Int'];
  /** The number of bakers removed during the specified period. */
  bakersRemoved: Scalars['Int'];
  /** Bucket-wise data for bakers added, removed, and the bucket times. */
  buckets: BakerMetricsBuckets;
  /** Total bakers before the start of the period */
  lastBakerCount: Scalars['Int'];
};

export type BakerMetricsBuckets = {
  __typename?: 'BakerMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /**
   * The time values (start of each bucket) intended for use as x-axis
   * values.
   */
  x_Time: Array<Scalars['DateTime']>;
  /**
   * The number of bakers added for each bucket, intended for use as y-axis
   * values.
   */
  y_BakersAdded: Array<Scalars['Int']>;
  /**
   * The number of bakers removed for each bucket, intended for use as y-axis
   * values.
   */
  y_BakersRemoved: Array<Scalars['Int']>;
  /** Total bakers during each period */
  y_LastBakerCount: Array<Scalars['Int']>;
};

export type BakerPool = {
  __typename?: 'BakerPool';
  apy: PoolApy;
  commissionRates: CommissionRates;
  delegatedStake: Scalars['UnsignedLong'];
  delegatedStakeCap: Scalars['UnsignedLong'];
  delegatorCount: Scalars['Int'];
  delegators: DelegationSummaryConnection;
  inactiveSuspended?: Maybe<Scalars['Int']>;
  lotteryPower: Scalars['Decimal'];
  metadataUrl?: Maybe<Scalars['String']>;
  openStatus?: Maybe<BakerPoolOpenStatus>;
  paydayCommissionRates?: Maybe<CommissionRates>;
  poolRewards: PaydayPoolRewardConnection;
  primedForSuspension?: Maybe<Scalars['Int']>;
  rankingByTotalStake?: Maybe<Ranking>;
  selfSuspended?: Maybe<Scalars['Int']>;
  totalStake: Scalars['UnsignedLong'];
  totalStakePercentage: Scalars['Decimal'];
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
  bakerId: Scalars['Long'];
};

export type BakerResumed = {
  __typename?: 'BakerResumed';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
};

export type BakerSetBakingRewardCommission = {
  __typename?: 'BakerSetBakingRewardCommission';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  bakingRewardCommission: Scalars['Decimal'];
};

export type BakerSetFinalizationRewardCommission = {
  __typename?: 'BakerSetFinalizationRewardCommission';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  finalizationRewardCommission: Scalars['Decimal'];
};

export type BakerSetMetadataUrl = {
  __typename?: 'BakerSetMetadataURL';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  metadataUrl: Scalars['String'];
};

export type BakerSetOpenStatus = {
  __typename?: 'BakerSetOpenStatus';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  openStatus: BakerPoolOpenStatus;
};

export type BakerSetRestakeEarnings = {
  __typename?: 'BakerSetRestakeEarnings';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  restakeEarnings: Scalars['Boolean'];
};

export type BakerSetTransactionFeeCommission = {
  __typename?: 'BakerSetTransactionFeeCommission';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  transactionFeeCommission: Scalars['Decimal'];
};

export enum BakerSort {
  BakerApy30DaysDesc = 'BAKER_APY30_DAYS_DESC',
  BakerIdAsc = 'BAKER_ID_ASC',
  BakerIdDesc = 'BAKER_ID_DESC',
  /** Sort ascending by the current payday baking commission rate. */
  BlockCommissionsAsc = 'BLOCK_COMMISSIONS_ASC',
  /** Sort descending by the current payday baking commission rate. */
  BlockCommissionsDesc = 'BLOCK_COMMISSIONS_DESC',
  DelegatorApy30DaysDesc = 'DELEGATOR_APY30_DAYS_DESC',
  DelegatorCountDesc = 'DELEGATOR_COUNT_DESC',
  TotalStakedAmountDesc = 'TOTAL_STAKED_AMOUNT_DESC'
}

export type BakerStakeDecreased = {
  __typename?: 'BakerStakeDecreased';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type BakerStakeIncreased = {
  __typename?: 'BakerStakeIncreased';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type BakerStakeThresholdChainUpdatePayload = {
  __typename?: 'BakerStakeThresholdChainUpdatePayload';
  amount: Scalars['UnsignedLong'];
};

export type BakerState = ActiveBakerState | RemovedBakerState;

export type BakerSuspended = {
  __typename?: 'BakerSuspended';
  accountAddress: AccountAddress;
  bakerId: Scalars['Long'];
};

export type BakingRewardCommissionNotInRange = {
  __typename?: 'BakingRewardCommissionNotInRange';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type BakingRewardsSpecialEvent = {
  __typename?: 'BakingRewardsSpecialEvent';
  bakingRewards: AccountAddressAmountConnection;
  id: Scalars['ID'];
  remainder: Scalars['UnsignedLong'];
};


export type BakingRewardsSpecialEventBakingRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type Block = {
  __typename?: 'Block';
  bakerId?: Maybe<Scalars['Long']>;
  blockHash: Scalars['String'];
  blockHeight: Scalars['Int'];
  /** Time of the block being baked. */
  blockSlotTime: Scalars['DateTime'];
  /**
   * The block statistics:
   * - The time difference from the parent block.
   * - The time difference to the block that justifies the block being
   * finalized.
   */
  blockStatistics: BlockStatistics;
  /** Whether the block is finalized. */
  finalized: Scalars['Boolean'];
  /** Absolute block height. */
  id: Scalars['ID'];
  /**
   * Query the special events (aka. special transaction outcomes) associated
   * with this block.
   */
  specialEvents: SpecialEventConnection;
  totalAmount: Scalars['UnsignedLong'];
  /** Number of transactions included in this block. */
  transactionCount: Scalars['Int'];
  transactions: TransactionConnection;
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
  bakerId: Scalars['Long'];
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

export type BlockConnection = {
  __typename?: 'BlockConnection';
  /** A list of edges. */
  edges: Array<BlockEdge>;
  /** A list of nodes. */
  nodes: Array<Block>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type BlockEdge = {
  __typename?: 'BlockEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Block;
};

export type BlockEnergyLimitUpdate = {
  __typename?: 'BlockEnergyLimitUpdate';
  energyLimit: Scalars['UnsignedLong'];
};

export type BlockMetrics = {
  __typename?: 'BlockMetrics';
  /**
   * The average block time in seconds (slot-time difference between two
   * adjacent blocks) in the requested period. Will be null if no blocks
   * have been added in the requested period.
   */
  avgBlockTime?: Maybe<Scalars['Float']>;
  /**
   * The average finalization time in seconds (slot-time difference between a
   * given block and the block that holds its finalization proof) in the
   * requested period. Will be null if no blocks have been finalized in
   * the requested period.
   */
  avgFinalizationTime?: Maybe<Scalars['Float']>;
  /** Total number of blocks added in requested period. */
  blocksAdded: Scalars['Int'];
  buckets: BlockMetricsBuckets;
  /**
   * The most recent block height. Equals the total length of the chain minus
   * one (genesis block is at height zero).
   */
  lastBlockHeight: Scalars['Int'];
  /** The current total amount of CCD in existence. */
  lastTotalMicroCcd: Scalars['UnsignedLong'];
  /**
   * The total CCD Released. This is total CCD supply not counting the
   * balances of non circulating accounts.
   */
  lastTotalMicroCcdReleased: Scalars['UnsignedLong'];
  /** The current total amount of CCD staked. */
  lastTotalMicroCcdStaked: Scalars['UnsignedLong'];
};

export type BlockMetricsBuckets = {
  __typename?: 'BlockMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /**
   * The average block time (slot-time difference between two adjacent
   * blocks) in the bucket period. Intended y-axis value. Will be null if
   * no blocks have been added in the bucket period.
   */
  y_BlockTimeAvg: Array<Scalars['Float']>;
  /**
   * Number of blocks added within the bucket time period. Intended y-axis
   * value.
   */
  y_BlocksAdded: Array<Scalars['Int']>;
  /**
   * The average finalization time (slot-time difference between a given
   * block and the block that holds its finalization proof) in the bucket
   * period. Intended y-axis value. Will be null if no blocks have been
   * finalized in the bucket period.
   */
  y_FinalizationTimeAvg: Array<Scalars['Float']>;
  /**
   * The total amount of CCD staked at the end of the bucket period. Intended
   * y-axis value.
   */
  y_LastTotalMicroCcdStaked: Array<Scalars['UnsignedLong']>;
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
  /**
   * Number of seconds between block slot time of this block and previous
   * block.
   */
  blockTime: Scalars['Float'];
  /**
   * Number of seconds between the block slot time of this block and the
   * block containing the finalization proof for this block.
   *
   * This is an objective measure of the finalization time (determined by
   * chain data alone) and will at least be the block time. The actual
   * finalization time will usually be lower than that but can only be
   * determined in a subjective manner by each node: That is the time a
   * node has first seen a block finalized. This is defined as the
   * difference between when a finalization proof is first constructed,
   * and the block slot time. However the time when a finalization proof
   * is first constructed is subjective, some nodes will receive the
   * necessary messages before others. Also, this number cannot be
   * reconstructed for blocks finalized before extracting data from the
   * node.
   *
   * Value will initially be `None` until the block containing the
   * finalization proof for this block is itself finalized.
   */
  finalizationTime?: Maybe<Scalars['Float']>;
};

export type BurnEvent = {
  __typename?: 'BurnEvent';
  amount: TokenAmount;
  target: TokenHolder;
};

export type CborHolderAccount = {
  __typename?: 'CborHolderAccount';
  address: AccountAddress;
  coinInfo?: Maybe<CoinInfo>;
};

export type ChainParametersV1 = {
  __typename?: 'ChainParametersV1';
  rewardPeriodLength: Scalars['UnsignedLong'];
};

export type ChainUpdateEnqueued = {
  __typename?: 'ChainUpdateEnqueued';
  effectiveTime: Scalars['DateTime'];
  payload: ChainUpdatePayload;
};

export type ChainUpdatePayload = AddAnonymityRevokerChainUpdatePayload | AddIdentityProviderChainUpdatePayload | BakerStakeThresholdChainUpdatePayload | BlockEnergyLimitUpdate | CooldownParametersChainUpdatePayload | CreatePltUpdate | ElectionDifficultyChainUpdatePayload | EuroPerEnergyChainUpdatePayload | FinalizationCommitteeParametersUpdate | FoundationAccountChainUpdatePayload | GasRewardsChainUpdatePayload | GasRewardsCpv2Update | Level1KeysChainUpdatePayload | MicroCcdPerEuroChainUpdatePayload | MinBlockTimeUpdate | MintDistributionChainUpdatePayload | MintDistributionV1ChainUpdatePayload | PoolParametersChainUpdatePayload | ProtocolChainUpdatePayload | RootKeysChainUpdatePayload | TimeParametersChainUpdatePayload | TimeoutParametersUpdate | TransactionFeeDistributionChainUpdatePayload | ValidatorScoreParametersUpdate;

export type Cis2Event = {
  __typename?: 'Cis2Event';
  contractIndex: Scalars['Int'];
  contractSubIndex: Scalars['Int'];
  event: CisEvent;
  indexPerToken: Scalars['Int'];
  tokenId: Scalars['String'];
  transaction: Transaction;
  transactionIndex: Scalars['Int'];
};

export type CisBurnEvent = {
  __typename?: 'CisBurnEvent';
  fromAddress: Address;
  tokenAmount: Scalars['BigInteger'];
  tokenId: Scalars['String'];
};

export type CisEvent = CisBurnEvent | CisMintEvent | CisTokenMetadataEvent | CisTransferEvent | CisUnknownEvent;

export type CisMintEvent = {
  __typename?: 'CisMintEvent';
  toAddress: Address;
  tokenAmount: Scalars['BigInteger'];
  tokenId: Scalars['String'];
};

export type CisTokenMetadataEvent = {
  __typename?: 'CisTokenMetadataEvent';
  hashHex?: Maybe<Scalars['String']>;
  metadataUrl: Scalars['String'];
  tokenId: Scalars['String'];
};

export type CisTransferEvent = {
  __typename?: 'CisTransferEvent';
  fromAddress: Address;
  toAddress: Address;
  tokenAmount: Scalars['BigInteger'];
  tokenId: Scalars['String'];
};

export type CisUnknownEvent = {
  __typename?: 'CisUnknownEvent';
  dummy: Scalars['UnsignedLong'];
};

export type CoinInfo = {
  __typename?: 'CoinInfo';
  coinInfoCode: Scalars['String'];
};

/** Information about the offset pagination. */
export type CollectionSegmentInfo = {
  __typename?: 'CollectionSegmentInfo';
  /**
   * Indicates whether more items exist following the set defined by the
   * clients arguments.
   */
  hasNextPage: Scalars['Boolean'];
  /**
   * Indicates whether more items exist prior the set defined by the clients
   * arguments.
   */
  hasPreviousPage: Scalars['Boolean'];
};

export type CommissionRange = {
  __typename?: 'CommissionRange';
  max: Scalars['Decimal'];
  min: Scalars['Decimal'];
};

export type CommissionRates = {
  __typename?: 'CommissionRates';
  bakingCommission?: Maybe<Scalars['Decimal']>;
  finalizationCommission?: Maybe<Scalars['Decimal']>;
  transactionCommission?: Maybe<Scalars['Decimal']>;
};

export type Contract = {
  __typename?: 'Contract';
  blockHeight: Scalars['Int'];
  blockSlotTime: Scalars['DateTime'];
  contractAddress: Scalars['String'];
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  contractEvents: ContractEventsCollectionSegment;
  contractRejectEvents: ContractRejectEventsCollectionSegment;
  creator: AccountAddress;
  snapshot: ContractSnapshot;
  tokens: TokensCollectionSegment;
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

export type ContractConnection = {
  __typename?: 'ContractConnection';
  /** A list of edges. */
  edges: Array<ContractEdge>;
  /** A list of nodes. */
  nodes: Array<Contract>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type ContractEdge = {
  __typename?: 'ContractEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Contract;
};

export type ContractEvent = {
  __typename?: 'ContractEvent';
  blockHeight: Scalars['Int'];
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
  items: Array<ContractEvent>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export type ContractInitialized = {
  __typename?: 'ContractInitialized';
  amount: Scalars['UnsignedLong'];
  contractAddress: ContractAddress;
  contractLogsRaw: Array<Array<Scalars['Int']>>;
  events: StringConnection;
  eventsAsHex: StringConnection;
  initName: Scalars['String'];
  inputParameter?: Maybe<Array<Scalars['Int']>>;
  message?: Maybe<Scalars['String']>;
  messageAsHex?: Maybe<Scalars['String']>;
  moduleRef: Scalars['String'];
  version: ContractVersion;
};

export type ContractInterrupted = {
  __typename?: 'ContractInterrupted';
  contractAddress: ContractAddress;
  contractLogsRaw: Array<Array<Scalars['Int']>>;
  events: StringConnection;
  eventsAsHex: StringConnection;
};

export type ContractModuleDeployed = {
  __typename?: 'ContractModuleDeployed';
  moduleRef: Scalars['String'];
};

export type ContractRejectEvent = {
  __typename?: 'ContractRejectEvent';
  blockSlotTime: Scalars['DateTime'];
  rejectedEvent: TransactionRejectReason;
  transactionHash: Scalars['String'];
};

/** A segment of a collection. */
export type ContractRejectEventsCollectionSegment = {
  __typename?: 'ContractRejectEventsCollectionSegment';
  /** A flattened list of the items. */
  items: Array<ContractRejectEvent>;
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
  blockHeight: Scalars['Int'];
  contractAddressIndex: Scalars['UnsignedLong'];
  contractAddressSubIndex: Scalars['UnsignedLong'];
  contractName: Scalars['String'];
  moduleReference: Scalars['String'];
};

export type ContractUpdated = {
  __typename?: 'ContractUpdated';
  amount: Scalars['UnsignedLong'];
  contractAddress: ContractAddress;
  contractLogsRaw: Array<Array<Scalars['Int']>>;
  events: StringConnection;
  eventsAsHex: StringConnection;
  inputParameter: Array<Scalars['Int']>;
  instigator: Address;
  message?: Maybe<Scalars['String']>;
  messageAsHex: Scalars['String'];
  receiveName: Scalars['String'];
  version: ContractVersion;
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

export type CooldownParametersChainUpdatePayload = {
  __typename?: 'CooldownParametersChainUpdatePayload';
  delegatorCooldown: Scalars['UnsignedLong'];
  poolOwnerCooldown: Scalars['UnsignedLong'];
};

export type CreatePlt = {
  __typename?: 'CreatePlt';
  /**
   * The number of decimal places used in the representation of amounts of
   * this token. This determines the smallest representable fraction of the
   * token.
   */
  decimals: Scalars['Int'];
  /** The initialization parameters of the token, encoded in CBOR. */
  initializationParameters: InitializationParameters;
  /** The symbol of the token. */
  tokenId: Scalars['String'];
  /** A SHA256 hash that identifies the token module implementation. */
  tokenModule: Scalars['String'];
};

export type CreatePltUpdate = {
  __typename?: 'CreatePltUpdate';
  /**
   * The number of decimal places used in the representation of amounts of
   * this token. This determines the smallest representable fraction of
   * the token. This can be at most 255.
   */
  decimals: Scalars['Int'];
  /** The initialization parameters of the token, encoded in CBOR. */
  initializationParameters: Scalars['JSON'];
  /** The token symbol. */
  tokenId: Scalars['String'];
  /** The hash that identifies the token module implementation. */
  tokenModule: Scalars['String'];
};

export type CredentialDeployed = {
  __typename?: 'CredentialDeployed';
  accountAddress: AccountAddress;
  regId: Scalars['String'];
};

export type CredentialDeploymentTransaction = {
  __typename?: 'CredentialDeploymentTransaction';
  credentialDeploymentTransactionType: CredentialDeploymentTransactionType;
};

export enum CredentialDeploymentTransactionType {
  Initial = 'INITIAL',
  Normal = 'NORMAL'
}

export type CredentialHolderDidNotSign = {
  __typename?: 'CredentialHolderDidNotSign';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
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
  delegatorId: Scalars['Int'];
  restakeEarnings: Scalars['Boolean'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type DelegationAdded = {
  __typename?: 'DelegationAdded';
  accountAddress: AccountAddress;
  delegatorId: Scalars['Int'];
};

export type DelegationRemoved = {
  __typename?: 'DelegationRemoved';
  accountAddress: AccountAddress;
  delegatorId: Scalars['Int'];
};

export type DelegationSetDelegationTarget = {
  __typename?: 'DelegationSetDelegationTarget';
  accountAddress: AccountAddress;
  delegationTarget: DelegationTarget;
  delegatorId: Scalars['Int'];
};

export type DelegationSetRestakeEarnings = {
  __typename?: 'DelegationSetRestakeEarnings';
  accountAddress: AccountAddress;
  delegatorId: Scalars['Int'];
  restakeEarnings: Scalars['Boolean'];
};

export type DelegationStakeDecreased = {
  __typename?: 'DelegationStakeDecreased';
  accountAddress: AccountAddress;
  delegatorId: Scalars['Int'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type DelegationStakeIncreased = {
  __typename?: 'DelegationStakeIncreased';
  accountAddress: AccountAddress;
  delegatorId: Scalars['Int'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type DelegationSummary = {
  __typename?: 'DelegationSummary';
  accountAddress: AccountAddress;
  restakeEarnings: Scalars['Boolean'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type DelegationSummaryConnection = {
  __typename?: 'DelegationSummaryConnection';
  /** A list of edges. */
  edges: Array<DelegationSummaryEdge>;
  /** A list of nodes. */
  nodes: Array<DelegationSummary>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type DelegationSummaryEdge = {
  __typename?: 'DelegationSummaryEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: DelegationSummary;
};

export type DelegationTarget = BakerDelegationTarget | PassiveDelegationTarget;

export type DelegationTargetNotABaker = {
  __typename?: 'DelegationTargetNotABaker';
  bakerId: Scalars['Long'];
};

export type DelegatorInCooldown = {
  __typename?: 'DelegatorInCooldown';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type DuplicateAggregationKey = {
  __typename?: 'DuplicateAggregationKey';
  aggregationKey: Scalars['String'];
};

export type DuplicateCredIds = {
  __typename?: 'DuplicateCredIds';
  credIds: Array<Scalars['String']>;
};

export type ElectionDifficultyChainUpdatePayload = {
  __typename?: 'ElectionDifficultyChainUpdatePayload';
  electionDifficulty: Scalars['Decimal'];
};

export type EncryptedAmountSelfTransfer = {
  __typename?: 'EncryptedAmountSelfTransfer';
  accountAddress: AccountAddress;
};

export type EncryptedAmountsRemoved = {
  __typename?: 'EncryptedAmountsRemoved';
  accountAddress: AccountAddress;
  inputAmount: Scalars['String'];
  newEncryptedAmount: Scalars['String'];
  upToIndex: Scalars['Int'];
};

export type EncryptedSelfAmountAdded = {
  __typename?: 'EncryptedSelfAmountAdded';
  accountAddress: AccountAddress;
  amount: Scalars['UnsignedLong'];
  newEncryptedAmount: Scalars['String'];
};

export type EuroPerEnergyChainUpdatePayload = {
  __typename?: 'EuroPerEnergyChainUpdatePayload';
  exchangeRate: Ratio;
};

export type Event = AccountCreated | AmountAddedByDecryption | BakerAdded | BakerDelegationRemoved | BakerKeysUpdated | BakerRemoved | BakerResumed | BakerSetBakingRewardCommission | BakerSetFinalizationRewardCommission | BakerSetMetadataUrl | BakerSetOpenStatus | BakerSetRestakeEarnings | BakerSetTransactionFeeCommission | BakerStakeDecreased | BakerStakeIncreased | BakerSuspended | ChainUpdateEnqueued | ContractCall | ContractInitialized | ContractInterrupted | ContractModuleDeployed | ContractResumed | ContractUpdated | ContractUpgraded | CredentialDeployed | CredentialKeysUpdated | CredentialsUpdated | DataRegistered | DelegationAdded | DelegationRemoved | DelegationSetDelegationTarget | DelegationSetRestakeEarnings | DelegationStakeDecreased | DelegationStakeIncreased | EncryptedAmountsRemoved | EncryptedSelfAmountAdded | NewEncryptedAmount | TokenCreationDetails | TokenUpdate | TransferMemo | Transferred | TransferredWithSchedule;

export type EventConnection = {
  __typename?: 'EventConnection';
  /** A list of edges. */
  edges: Array<EventEdge>;
  /** A list of nodes. */
  nodes: Array<Event>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
  totalCount: Scalars['Int'];
};

/** An edge in a connection. */
export type EventEdge = {
  __typename?: 'EventEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Event;
};

export type FinalizationCommitteeParametersUpdate = {
  __typename?: 'FinalizationCommitteeParametersUpdate';
  finalizersRelativeStakeThreshold: Scalars['Decimal'];
  maxFinalizers: Scalars['UnsignedInt'];
  minFinalizers: Scalars['UnsignedInt'];
};

export type FinalizationRewardCommissionNotInRange = {
  __typename?: 'FinalizationRewardCommissionNotInRange';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type FinalizationRewardsSpecialEvent = {
  __typename?: 'FinalizationRewardsSpecialEvent';
  finalizationRewards: AccountAddressAmountConnection;
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
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type FoundationAccountChainUpdatePayload = {
  __typename?: 'FoundationAccountChainUpdatePayload';
  accountAddress: AccountAddress;
};

export type GasRewardsChainUpdatePayload = {
  __typename?: 'GasRewardsChainUpdatePayload';
  accountCreation: Scalars['Decimal'];
  baker: Scalars['Decimal'];
  chainUpdate: Scalars['Decimal'];
  finalizationProof: Scalars['Decimal'];
};

export type GasRewardsCpv2Update = {
  __typename?: 'GasRewardsCpv2Update';
  accountCreation: Scalars['Decimal'];
  baker: Scalars['Decimal'];
  chainUpdate: Scalars['Decimal'];
};

/**
 * Represents protocol-level token (PLT) metrics for a given period.
 *
 * This struct is returned by the GraphQL API and provides summary statistics
 * for PLT token activity over a specified time window.
 */
export type GlobalPltMetrics = {
  __typename?: 'GlobalPltMetrics';
  eventCount: Scalars['Int'];
  transferAmount: Scalars['Float'];
};

export type HoldingResponse = {
  __typename?: 'HoldingResponse';
  address: Scalars['String'];
  assetName: Scalars['String'];
  percentage: Scalars['Float'];
  quantity: Scalars['Float'];
};

export type ImportState = {
  __typename?: 'ImportState';
  epochDuration: Scalars['TimeSpan'];
};

export type InitializationParameters = {
  __typename?: 'InitializationParameters';
  allowList?: Maybe<Scalars['Boolean']>;
  burnable?: Maybe<Scalars['Boolean']>;
  denyList?: Maybe<Scalars['Boolean']>;
  governanceAccount: CborHolderAccount;
  initialSupply?: Maybe<TokenAmount>;
  metadata: MetadataUrl;
  mintable?: Maybe<Scalars['Boolean']>;
  name: Scalars['String'];
};

/**
 * The status of parsing `message` into its JSON representation using the
 * smart contract module schema.
 */
export enum InstanceMessageParsingStatus {
  /** Relevant smart contract not found in smart contract module schema. */
  ContractNotFound = 'CONTRACT_NOT_FOUND',
  /** No message was provided. */
  EmptyMessage = 'EMPTY_MESSAGE',
  /**
   * Failed to construct the JSON representation from message using the smart
   * contract schema.
   */
  Failed = 'FAILED',
  /** Relevant smart contract function not found in smart contract schema. */
  FunctionNotFound = 'FUNCTION_NOT_FOUND',
  /** No module schema found in the deployed smart contract module. */
  ModuleSchemaNotFound = 'MODULE_SCHEMA_NOT_FOUND',
  /** Schema for parameter not found in smart contract schema. */
  ParamNotFound = 'PARAM_NOT_FOUND',
  /** Parsing succeeded. */
  Success = 'SUCCESS'
}

export type InsufficientBalanceForBakerStake = {
  __typename?: 'InsufficientBalanceForBakerStake';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InsufficientBalanceForDelegationStake = {
  __typename?: 'InsufficientBalanceForDelegationStake';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InsufficientDelegationStake = {
  __typename?: 'InsufficientDelegationStake';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InterimTransaction = {
  __typename?: 'InterimTransaction';
  transaction: Transaction;
};

export type InterimTransactionConnection = {
  __typename?: 'InterimTransactionConnection';
  /** A list of edges. */
  edges: Array<InterimTransactionEdge>;
  /** A list of nodes. */
  nodes: Array<InterimTransaction>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type InterimTransactionEdge = {
  __typename?: 'InterimTransactionEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: InterimTransaction;
};

export type InvalidAccountReference = {
  __typename?: 'InvalidAccountReference';
  accountAddress: AccountAddress;
};

export type InvalidAccountThreshold = {
  __typename?: 'InvalidAccountThreshold';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidContractAddress = {
  __typename?: 'InvalidContractAddress';
  contractAddress: ContractAddress;
};

export type InvalidCredentialKeySignThreshold = {
  __typename?: 'InvalidCredentialKeySignThreshold';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidCredentials = {
  __typename?: 'InvalidCredentials';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidEncryptedAmountTransferProof = {
  __typename?: 'InvalidEncryptedAmountTransferProof';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidIndexOnEncryptedTransfer = {
  __typename?: 'InvalidIndexOnEncryptedTransfer';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidInitMethod = {
  __typename?: 'InvalidInitMethod';
  initName: Scalars['String'];
  moduleRef: Scalars['String'];
};

export type InvalidModuleReference = {
  __typename?: 'InvalidModuleReference';
  moduleRef: Scalars['String'];
};

export type InvalidProof = {
  __typename?: 'InvalidProof';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidReceiveMethod = {
  __typename?: 'InvalidReceiveMethod';
  moduleRef: Scalars['String'];
  receiveName: Scalars['String'];
};

export type InvalidTransferToPublicProof = {
  __typename?: 'InvalidTransferToPublicProof';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type KeyIndexAlreadyInUse = {
  __typename?: 'KeyIndexAlreadyInUse';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type LatestChainParameters = ChainParametersV1;

export type LatestTransactionResponse = {
  __typename?: 'LatestTransactionResponse';
  amount: Scalars['Float'];
  assetMetadata?: Maybe<Metadata>;
  assetName: Scalars['String'];
  dateTime: Scalars['DateTime'];
  from: Scalars['String'];
  to: Scalars['String'];
  transactionHash: Scalars['String'];
  value: Scalars['Float'];
};

export type Level1KeysChainUpdatePayload = {
  __typename?: 'Level1KeysChainUpdatePayload';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
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
  items: Array<LinkedContract>;
  totalCount: Scalars['Int'];
};

export type Memo = {
  __typename?: 'Memo';
  bytes: Scalars['String'];
};

export type Metadata = {
  __typename?: 'Metadata';
  iconUrl: Scalars['String'];
};

export type MetadataUrl = {
  __typename?: 'MetadataUrl';
  additional?: Maybe<Scalars['JSON']>;
  checksumSha256?: Maybe<Scalars['String']>;
  url: Scalars['String'];
};

export enum MetricsPeriod {
  Last7Days = 'LAST7_DAYS',
  Last24Hours = 'LAST24_HOURS',
  Last30Days = 'LAST30_DAYS',
  Last90Days = 'LAST90_DAYS',
  LastHour = 'LAST_HOUR',
  LastYear = 'LAST_YEAR'
}

export type MicroCcdPerEuroChainUpdatePayload = {
  __typename?: 'MicroCcdPerEuroChainUpdatePayload';
  exchangeRate: Ratio;
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

export type MintDistributionV1ChainUpdatePayload = {
  __typename?: 'MintDistributionV1ChainUpdatePayload';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
};

export type MintEvent = {
  __typename?: 'MintEvent';
  amount: TokenAmount;
  target: TokenHolder;
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
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type MissingDelegationAddParameters = {
  __typename?: 'MissingDelegationAddParameters';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ModuleHashAlreadyExists = {
  __typename?: 'ModuleHashAlreadyExists';
  moduleRef: Scalars['String'];
};

export type ModuleNotWf = {
  __typename?: 'ModuleNotWf';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export enum ModuleReferenceContractLinkAction {
  Added = 'ADDED',
  Removed = 'REMOVED'
}

export type ModuleReferenceContractLinkEvent = {
  __typename?: 'ModuleReferenceContractLinkEvent';
  blockSlotTime: Scalars['DateTime'];
  contractAddress: ContractAddress;
  linkAction: ModuleReferenceContractLinkAction;
  transactionHash: Scalars['String'];
};

/** A segment of a collection. */
export type ModuleReferenceContractLinkEventsCollectionSegment = {
  __typename?: 'ModuleReferenceContractLinkEventsCollectionSegment';
  /** A flattened list of the items. */
  items: Array<ModuleReferenceContractLinkEvent>;
  totalCount: Scalars['Int'];
};

export type ModuleReferenceEvent = {
  __typename?: 'ModuleReferenceEvent';
  blockHeight: Scalars['Int'];
  blockSlotTime: Scalars['DateTime'];
  displaySchema?: Maybe<Scalars['String']>;
  linkedContracts: LinkedContractsCollectionSegment;
  moduleReference: Scalars['String'];
  moduleReferenceContractLinkEvents: ModuleReferenceContractLinkEventsCollectionSegment;
  moduleReferenceRejectEvents: ModuleReferenceRejectEventsCollectionSegment;
  sender: AccountAddress;
  transactionHash: Scalars['String'];
  transactionIndex: Scalars['Int'];
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

export type ModuleReferenceEventConnection = {
  __typename?: 'ModuleReferenceEventConnection';
  /** A list of edges. */
  edges: Array<ModuleReferenceEventEdge>;
  /** A list of nodes. */
  nodes: Array<ModuleReferenceEvent>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type ModuleReferenceEventEdge = {
  __typename?: 'ModuleReferenceEventEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: ModuleReferenceEvent;
};

export type ModuleReferenceRejectEvent = {
  __typename?: 'ModuleReferenceRejectEvent';
  blockHeight: Scalars['Int'];
  blockSlotTime: Scalars['DateTime'];
  moduleReference: Scalars['String'];
  rejectedEvent: TransactionRejectReason;
  transactionHash: Scalars['String'];
};

export type ModuleReferenceRejectEventsCollectionSegment = {
  __typename?: 'ModuleReferenceRejectEventsCollectionSegment';
  items: Array<ModuleReferenceRejectEvent>;
  totalCount: Scalars['Int'];
};

export type NewEncryptedAmount = {
  __typename?: 'NewEncryptedAmount';
  accountAddress: AccountAddress;
  encryptedAmount: Scalars['String'];
  newIndex: Scalars['Int'];
};

export enum NodeSortDirection {
  Asc = 'ASC',
  Desc = 'DESC'
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
  bestArrivedTime?: Maybe<Scalars['String']>;
  bestBlock: Scalars['String'];
  bestBlockBakerId?: Maybe<Scalars['Int']>;
  bestBlockCentralBankAmount?: Maybe<Scalars['Int']>;
  bestBlockExecutionCost?: Maybe<Scalars['Int']>;
  bestBlockHeight: Scalars['Int'];
  bestBlockTotalAmount?: Maybe<Scalars['Int']>;
  bestBlockTotalEncryptedAmount?: Maybe<Scalars['Int']>;
  bestBlockTransactionCount?: Maybe<Scalars['Int']>;
  bestBlockTransactionEnergyCost?: Maybe<Scalars['Int']>;
  bestBlockTransactionsSize?: Maybe<Scalars['Int']>;
  blockArriveLatencyEma?: Maybe<Scalars['Float']>;
  blockArriveLatencyEmsd?: Maybe<Scalars['Float']>;
  blockArrivePeriodEma?: Maybe<Scalars['Float']>;
  blockArrivePeriodEmsd?: Maybe<Scalars['Float']>;
  blockReceiveLatencyEma?: Maybe<Scalars['Float']>;
  blockReceiveLatencyEmsd?: Maybe<Scalars['Float']>;
  blockReceivePeriodEma?: Maybe<Scalars['Float']>;
  blockReceivePeriodEmsd?: Maybe<Scalars['Float']>;
  blocksReceivedCount?: Maybe<Scalars['Int']>;
  blocksVerifiedCount?: Maybe<Scalars['Int']>;
  clientVersion: Scalars['String'];
  consensusBakerId?: Maybe<Scalars['Int']>;
  consensusRunning: Scalars['Boolean'];
  finalizationCommitteeMember: Scalars['Boolean'];
  finalizationCount?: Maybe<Scalars['Int']>;
  finalizationPeriodEma?: Maybe<Scalars['Float']>;
  finalizationPeriodEmsd?: Maybe<Scalars['Float']>;
  finalizedBlock: Scalars['String'];
  finalizedBlockHeight: Scalars['Int'];
  finalizedBlockParent: Scalars['String'];
  finalizedTime?: Maybe<Scalars['String']>;
  genesisBlock: Scalars['String'];
  id: Scalars['ID'];
  nodeId: Scalars['String'];
  nodeName: Scalars['String'];
  packetsReceived: Scalars['Int'];
  packetsSent: Scalars['Int'];
  peerType: Scalars['String'];
  peersCount: Scalars['Int'];
  peersList: Array<PeerReference>;
  transactionsPerBlockEma?: Maybe<Scalars['Float']>;
  transactionsPerBlockEmsd?: Maybe<Scalars['Float']>;
  uptime: Scalars['Int'];
};

export type NodeStatusConnection = {
  __typename?: 'NodeStatusConnection';
  /** A list of edges. */
  edges: Array<NodeStatusEdge>;
  /** A list of nodes. */
  nodes: Array<NodeStatus>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type NodeStatusEdge = {
  __typename?: 'NodeStatusEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: NodeStatus;
};

export type NonExistentCredIds = {
  __typename?: 'NonExistentCredIds';
  credIds: Array<Scalars['String']>;
};

export type NonExistentCredentialId = {
  __typename?: 'NonExistentCredentialId';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NonExistentRewardAccount = {
  __typename?: 'NonExistentRewardAccount';
  accountAddress: AccountAddress;
};

export type NonExistentTokenId = {
  __typename?: 'NonExistentTokenId';
  tokenId: Scalars['String'];
};

export type NonIncreasingSchedule = {
  __typename?: 'NonIncreasingSchedule';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NotABaker = {
  __typename?: 'NotABaker';
  accountAddress: AccountAddress;
};

export type NotADelegator = {
  __typename?: 'NotADelegator';
  accountAddress: AccountAddress;
};

export type NotAllowedMultipleCredentials = {
  __typename?: 'NotAllowedMultipleCredentials';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NotAllowedToHandleEncrypted = {
  __typename?: 'NotAllowedToHandleEncrypted';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type NotAllowedToReceiveEncrypted = {
  __typename?: 'NotAllowedToReceiveEncrypted';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type OutOfEnergy = {
  __typename?: 'OutOfEnergy';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

/** Information about pagination in a connection */
export type PageInfo = {
  __typename?: 'PageInfo';
  /** When paginating forwards, the cursor to continue. */
  endCursor?: Maybe<Scalars['String']>;
  /** When paginating forwards, are there more items? */
  hasNextPage: Scalars['Boolean'];
  /** When paginating backwards, are there more items? */
  hasPreviousPage: Scalars['Boolean'];
  /** When paginating backwards, the cursor to continue. */
  startCursor?: Maybe<Scalars['String']>;
};

export type PassiveDelegation = {
  __typename?: 'PassiveDelegation';
  apy?: Maybe<Scalars['Float']>;
  commissionRates: CommissionRates;
  delegatedStake: Scalars['BigInteger'];
  /**
   * Total passively delegated stake as a percentage of all CCDs in
   * existence.
   */
  delegatedStakePercentage: Scalars['Decimal'];
  delegatorCount: Scalars['Int'];
  delegators: PassiveDelegationSummaryConnection;
  poolRewards: PaydayPoolRewardConnection;
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

export type PassiveDelegationSummary = {
  __typename?: 'PassiveDelegationSummary';
  accountAddress: AccountAddress;
  restakeEarnings: Scalars['Boolean'];
  stakedAmount: Scalars['UnsignedLong'];
};

export type PassiveDelegationSummaryConnection = {
  __typename?: 'PassiveDelegationSummaryConnection';
  /** A list of edges. */
  edges: Array<PassiveDelegationSummaryEdge>;
  /** A list of nodes. */
  nodes: Array<PassiveDelegationSummary>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PassiveDelegationSummaryEdge = {
  __typename?: 'PassiveDelegationSummaryEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: PassiveDelegationSummary;
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
  bakerReward: PaydayPoolRewardAmounts;
  block: Block;
  finalizationReward: PaydayPoolRewardAmounts;
  id: Scalars['Int'];
  poolOwner?: Maybe<Scalars['Int']>;
  timestamp: Scalars['DateTime'];
  transactionFees: PaydayPoolRewardAmounts;
};

export type PaydayPoolRewardAmounts = {
  __typename?: 'PaydayPoolRewardAmounts';
  bakerAmount: Scalars['Int'];
  delegatorsAmount: Scalars['Int'];
  totalAmount: Scalars['Int'];
};

export type PaydayPoolRewardConnection = {
  __typename?: 'PaydayPoolRewardConnection';
  /** A list of edges. */
  edges: Array<PaydayPoolRewardEdge>;
  /** A list of nodes. */
  nodes: Array<PaydayPoolReward>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PaydayPoolRewardEdge = {
  __typename?: 'PaydayPoolRewardEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
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
  paydaySummaries: PaydaySummaryConnection;
};


export type PaydayStatusPaydaySummariesArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type PaydaySummary = {
  __typename?: 'PaydaySummary';
  block: Block;
  blockHeight: Scalars['Int'];
};

export type PaydaySummaryConnection = {
  __typename?: 'PaydaySummaryConnection';
  /** A list of edges. */
  edges: Array<PaydaySummaryEdge>;
  /** A list of nodes. */
  nodes: Array<PaydaySummary>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PaydaySummaryEdge = {
  __typename?: 'PaydaySummaryEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: PaydaySummary;
};

export type Peer = {
  __typename?: 'Peer';
  id: Scalars['ID'];
  nodeId: Scalars['String'];
  nodeName: Scalars['String'];
};

export type PeerReference = {
  __typename?: 'PeerReference';
  nodeId: Scalars['String'];
  nodeStatus?: Maybe<Peer>;
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

export type PltAccountAmount = {
  __typename?: 'PltAccountAmount';
  accountAddress: AccountAddress;
  amount: TokenAmount;
  tokenId: Scalars['String'];
};

export type PltAccountAmountConnection = {
  __typename?: 'PltAccountAmountConnection';
  /** A list of edges. */
  edges: Array<PltAccountAmountEdge>;
  /** A list of nodes. */
  nodes: Array<PltAccountAmount>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PltAccountAmountEdge = {
  __typename?: 'PltAccountAmountEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: PltAccountAmount;
};

export type PltEvent = {
  __typename?: 'PltEvent';
  block: Block;
  eventType?: Maybe<TokenUpdateEventType>;
  id: Scalars['Int'];
  tokenEvent: TokenEventDetails;
  tokenId: Scalars['String'];
  tokenModuleType?: Maybe<TokenUpdateModuleType>;
  tokenName?: Maybe<Scalars['String']>;
  transactionHash: Scalars['String'];
  transactionIndex: Scalars['Int'];
};

export type PltEventConnection = {
  __typename?: 'PltEventConnection';
  /** A list of edges. */
  edges: Array<PltEventEdge>;
  /** A list of nodes. */
  nodes: Array<PltEvent>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PltEventEdge = {
  __typename?: 'PltEventEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: PltEvent;
};

export type PltToken = {
  __typename?: 'PltToken';
  block: Block;
  decimal?: Maybe<Scalars['Int']>;
  index: Scalars['Int'];
  initialSupply?: Maybe<Scalars['Int']>;
  issuer: AccountAddress;
  metadata?: Maybe<Scalars['JSON']>;
  moduleReference?: Maybe<Scalars['String']>;
  name?: Maybe<Scalars['String']>;
  tokenCreationDetails: TokenCreationDetails;
  tokenId: Scalars['String'];
  totalBurned?: Maybe<Scalars['Int']>;
  totalMinted?: Maybe<Scalars['Int']>;
  totalSupply?: Maybe<Scalars['Int']>;
  totalUniqueHolders: Scalars['Int'];
  transactionHash: Scalars['String'];
};

export type PltTokenConnection = {
  __typename?: 'PltTokenConnection';
  /** A list of edges. */
  edges: Array<PltTokenEdge>;
  /** A list of nodes. */
  nodes: Array<PltToken>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type PltTokenEdge = {
  __typename?: 'PltTokenEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: PltToken;
};

/** This struct is used to define the buckets for PLT transfer metrics. */
export type PltTransferMetricsBuckets = {
  __typename?: 'PltTransferMetricsBuckets';
  bucketWidth: Scalars['TimeSpan'];
  x_Time: Array<Scalars['DateTime']>;
  y_TransferAmount: Array<Scalars['Float']>;
  y_TransferCount: Array<Scalars['Int']>;
};

/** This struct is used to define the GraphQL query for PLT transfer metrics. */
export type PltTransferMetricsByTokenId = {
  __typename?: 'PltTransferMetricsByTokenId';
  buckets: PltTransferMetricsBuckets;
  decimal: Scalars['Int'];
  transferAmount: Scalars['Float'];
  transferCount: Scalars['Int'];
};

export type PoolApy = {
  __typename?: 'PoolApy';
  bakerApy?: Maybe<Scalars['Float']>;
  delegatorsApy?: Maybe<Scalars['Float']>;
  totalApy?: Maybe<Scalars['Float']>;
};

export type PoolClosed = {
  __typename?: 'PoolClosed';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
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
  /** Bucket-wise data for rewards */
  buckets: PoolRewardMetricsBuckets;
  /** Baker rewards at the end of the interval */
  sumBakerRewardAmount: Scalars['Long'];
  /** Delegator rewards at the end of the interval */
  sumDelegatorsRewardAmount: Scalars['Long'];
  /** Total rewards at the end of the interval */
  sumTotalRewardAmount: Scalars['Long'];
};

export type PoolRewardMetricsBuckets = {
  __typename?: 'PoolRewardMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  x_Time: Array<Scalars['DateTime']>;
  y_SumBakerRewards: Array<Scalars['Long']>;
  y_SumDelegatorsRewards: Array<Scalars['Long']>;
  y_SumTotalRewards: Array<Scalars['Long']>;
};

export type PoolRewardTarget = BakerPoolRewardTarget | PassiveDelegationPoolRewardTarget;

export type PoolWouldBecomeOverDelegated = {
  __typename?: 'PoolWouldBecomeOverDelegated';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ProtocolChainUpdatePayload = {
  __typename?: 'ProtocolChainUpdatePayload';
  message: Scalars['String'];
  specificationAuxiliaryDataHex: Scalars['String'];
  specificationHash: Scalars['String'];
  specificationUrl: Scalars['String'];
};

export type Query = {
  __typename?: 'Query';
  account: Account;
  accountByAddress: Account;
  accounts: AccountConnection;
  accountsMetrics: AccountsMetrics;
  baker: Baker;
  bakerByBakerId: Baker;
  /**
   * Fetches baker metrics for the specified period.
   *
   * This function queries the database for baker metrics such as the number
   * of bakers added, removed, and the last baker count in the specified
   * time period. It returns the results as a structured `BakerMetrics`
   * object.
   */
  bakerMetrics: BakerMetrics;
  bakers: BakerConnection;
  block: Block;
  blockByBlockHash: Block;
  blockMetrics: BlockMetrics;
  /** Query the list of blocks ordered descendingly by block height. */
  blocks: BlockConnection;
  contract: Contract;
  contracts: ContractConnection;
  /**
   * Query for PLT metrics over a specified time period. (across all plts)
   * returns GlobalPltMetrics plt event_count (Mint/Burn/Transfer etc)
   * and transfer_volume (the total volume of transfers normalized across all
   * plts by their respective decimals)
   */
  globalPltMetrics: GlobalPltMetrics;
  importState: ImportState;
  latestChainParameters: LatestChainParameters;
  latestTransactions?: Maybe<Array<LatestTransactionResponse>>;
  moduleReferenceEvent: ModuleReferenceEvent;
  nodeStatus?: Maybe<NodeStatus>;
  nodeStatuses: NodeStatusConnection;
  passiveDelegation: PassiveDelegation;
  paydayStatus: PaydayStatus;
  pltAccountByTokenId?: Maybe<PltAccountAmount>;
  pltAccountsByTokenId: PltAccountAmountConnection;
  pltEvent: PltEvent;
  pltEventByTransactionIndex: PltEvent;
  pltEvents: PltEventConnection;
  pltEventsByTokenId: PltEventConnection;
  pltToken: PltToken;
  pltTokens: PltTokenConnection;
  pltTransferMetricsByTokenId: PltTransferMetricsByTokenId;
  pltUniqueAccounts: Scalars['Int'];
  poolRewardMetricsForBakerPool: PoolRewardMetrics;
  poolRewardMetricsForPassiveDelegation: PoolRewardMetrics;
  rewardMetrics: RewardMetrics;
  rewardMetricsForAccount: RewardMetrics;
  search: SearchResult;
  stablecoin?: Maybe<StableCoin>;
  stablecoinOverview: StableCoinOverview;
  stablecoins: Array<StableCoin>;
  stablecoinsBySupply: Array<StableCoin>;
  suspendedValidators: SuspendedValidators;
  token: Token;
  tokens: TokenConnection;
  transaction: Transaction;
  transactionByTransactionHash: Transaction;
  transactionMetrics: TransactionMetrics;
  transactions: TransactionConnection;
  transferSummary: TransferSummaryResponse;
  versions: Versions;
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


export type QueryGlobalPltMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryLatestTransactionsArgs = {
  limit?: InputMaybe<Scalars['Int']>;
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


export type QueryPltAccountByTokenIdArgs = {
  account: Scalars['ID'];
  tokenId: Scalars['ID'];
};


export type QueryPltAccountsByTokenIdArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
  tokenId: Scalars['ID'];
};


export type QueryPltEventArgs = {
  id: Scalars['ID'];
};


export type QueryPltEventByTransactionIndexArgs = {
  transactionIndex: Scalars['ID'];
};


export type QueryPltEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type QueryPltEventsByTokenIdArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  id: Scalars['ID'];
  last?: InputMaybe<Scalars['Int']>;
};


export type QueryPltTokenArgs = {
  id: Scalars['ID'];
};


export type QueryPltTokensArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};


export type QueryPltTransferMetricsByTokenIdArgs = {
  period: MetricsPeriod;
  tokenId: Scalars['String'];
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


export type QueryStablecoinArgs = {
  lastNTransactions?: InputMaybe<Scalars['Int']>;
  limit?: InputMaybe<Scalars['Int']>;
  minQuantity?: InputMaybe<Scalars['Float']>;
  symbol: Scalars['String'];
};


export type QueryStablecoinsBySupplyArgs = {
  minSupply: Scalars['Int'];
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


export type QueryTransferSummaryArgs = {
  assetName: Scalars['String'];
  days?: InputMaybe<Scalars['Int']>;
};

/**
 * Ranking of the bakers by lottery powers from the last payday block staring
 * with rank 1 for the baker with the highest lottery power and ending with the
 * rank `total` for the baker with the lowest lottery power.
 */
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
  rejectReason: Scalars['Int'];
};

/** Transaction updating a smart contract instance was rejected. */
export type RejectedReceive = {
  __typename?: 'RejectedReceive';
  /** Address of the smart contract instance which rejected the update. */
  contractAddress: ContractAddress;
  /**
   * The JSON representation of the message provided for the smart contract
   * instance as parameter. Decoded using the smart contract module
   * schema if present otherwise undefined. Failing to parse the message
   * will result in this being undefined and `message_parsing_status`
   * representing the error.
   */
  message?: Maybe<Scalars['String']>;
  /**
   * The HEX representation of the message provided for the smart contract
   * instance as parameter.
   */
  messageAsHex: Scalars['String'];
  /**
   * The status of parsing `message` into its JSON representation using the
   * smart contract module schema.
   */
  messageParsingStatus: InstanceMessageParsingStatus;
  /**
   * The name of the entry point called in the smart contract instance (in
   * ReceiveName format '<contract_name>.<entrypoint>').
   */
  receiveName: Scalars['String'];
  /** Reject reason code produced by the smart contract instance. */
  rejectReason: Scalars['Int'];
};

export type RemoveFirstCredential = {
  __typename?: 'RemoveFirstCredential';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type RemovedBakerState = {
  __typename?: 'RemovedBakerState';
  removedAt: Scalars['DateTime'];
};

export type RewardMetrics = {
  __typename?: 'RewardMetrics';
  /** Bucket-wise data for rewards */
  buckets: RewardMetricsBuckets;
  /** Total rewards at the end of the interval */
  sumRewardAmount: Scalars['Int'];
};

export type RewardMetricsBuckets = {
  __typename?: 'RewardMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  x_Time: Array<Scalars['DateTime']>;
  y_SumRewards: Array<Scalars['Int']>;
};

export enum RewardType {
  BakerReward = 'BAKER_REWARD',
  FinalizationReward = 'FINALIZATION_REWARD',
  FoundationReward = 'FOUNDATION_REWARD',
  TransactionFeeReward = 'TRANSACTION_FEE_REWARD'
}

export type RootKeysChainUpdatePayload = {
  __typename?: 'RootKeysChainUpdatePayload';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type RuntimeFailure = {
  __typename?: 'RuntimeFailure';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ScheduledSelfTransfer = {
  __typename?: 'ScheduledSelfTransfer';
  accountAddress: AccountAddress;
};

export type SearchResult = {
  __typename?: 'SearchResult';
  accounts: AccountConnection;
  bakers: BakerConnection;
  blocks: BlockConnection;
  contracts: ContractConnection;
  modules: ModuleReferenceEventConnection;
  nodeStatuses: NodeStatusConnection;
  tokens: TokenConnection;
  transactions: TransactionConnection;
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
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type SpecialEvent = BakingRewardsSpecialEvent | BlockAccrueRewardSpecialEvent | BlockRewardsSpecialEvent | FinalizationRewardsSpecialEvent | MintSpecialEvent | PaydayAccountRewardSpecialEvent | PaydayFoundationRewardSpecialEvent | PaydayPoolRewardSpecialEvent | ValidatorPrimedForSuspension | ValidatorSuspended;

export type SpecialEventConnection = {
  __typename?: 'SpecialEventConnection';
  /** A list of edges. */
  edges: Array<SpecialEventEdge>;
  /** A list of nodes. */
  nodes: Array<SpecialEvent>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type SpecialEventEdge = {
  __typename?: 'SpecialEventEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: SpecialEvent;
};

export enum SpecialEventTypeFilter {
  BakingRewards = 'BAKING_REWARDS',
  BlockAccrueReward = 'BLOCK_ACCRUE_REWARD',
  BlockRewards = 'BLOCK_REWARDS',
  FinalizationRewards = 'FINALIZATION_REWARDS',
  Mint = 'MINT',
  PaydayAccountReward = 'PAYDAY_ACCOUNT_REWARD',
  PaydayFoundationReward = 'PAYDAY_FOUNDATION_REWARD',
  PaydayPoolReward = 'PAYDAY_POOL_REWARD',
  ValidatorPrimedForSuspension = 'VALIDATOR_PRIMED_FOR_SUSPENSION',
  ValidatorSuspended = 'VALIDATOR_SUSPENDED'
}

export type StableCoin = {
  __typename?: 'StableCoin';
  circulatingSupply: Scalars['Int'];
  decimal: Scalars['Int'];
  holdings?: Maybe<Array<HoldingResponse>>;
  issuer: Scalars['String'];
  metadata?: Maybe<Metadata>;
  name: Scalars['String'];
  symbol: Scalars['String'];
  totalSupply: Scalars['Int'];
  totalUniqueHolders?: Maybe<Scalars['Int']>;
  transactions?: Maybe<Array<TransactionMResponse>>;
  transfers?: Maybe<Array<Transfer>>;
  valueInDollar: Scalars['Float'];
};

export type StableCoinOverview = {
  __typename?: 'StableCoinOverview';
  noOfTxn: Scalars['Int'];
  noOfTxnLast24H: Scalars['Int'];
  numberOfUniqueHolders: Scalars['Int'];
  totalMarketcap: Scalars['Float'];
  valuesTransferred: Scalars['Float'];
  valuesTransferredLast24H: Scalars['Float'];
};

export type StakeOverMaximumThresholdForPool = {
  __typename?: 'StakeOverMaximumThresholdForPool';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type StakeUnderMinimumThresholdForBaking = {
  __typename?: 'StakeUnderMinimumThresholdForBaking';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type StringConnection = {
  __typename?: 'StringConnection';
  /** A list of edges. */
  edges: Array<StringEdge>;
  /** A list of nodes. */
  nodes: Array<Scalars['String']>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type StringEdge = {
  __typename?: 'StringEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Scalars['String'];
};

export type Subscription = {
  __typename?: 'Subscription';
  accountsUpdated: AccountsUpdatedSubscriptionItem;
  blockAdded: Block;
};


export type SubscriptionAccountsUpdatedArgs = {
  accountAddress?: InputMaybe<Scalars['String']>;
};

export type Success = {
  __typename?: 'Success';
  events: EventConnection;
};


export type SuccessEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
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

export type TimestampedAmountConnection = {
  __typename?: 'TimestampedAmountConnection';
  /** A list of edges. */
  edges: Array<TimestampedAmountEdge>;
  /** A list of nodes. */
  nodes: Array<TimestampedAmount>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type TimestampedAmountEdge = {
  __typename?: 'TimestampedAmountEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: TimestampedAmount;
};

export type Token = {
  __typename?: 'Token';
  accounts: AccountsCollectionSegment;
  contractAddressFormatted: Scalars['String'];
  contractIndex: Scalars['Int'];
  contractSubIndex: Scalars['Int'];
  initialTransaction: Transaction;
  metadataUrl?: Maybe<Scalars['String']>;
  tokenAddress: Scalars['String'];
  tokenEvents: TokenEventsCollectionSegment;
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

export type TokenAmount = {
  __typename?: 'TokenAmount';
  decimals: Scalars['String'];
  value: Scalars['String'];
};

export type TokenConnection = {
  __typename?: 'TokenConnection';
  /** A list of edges. */
  edges: Array<TokenEdge>;
  /** A list of nodes. */
  nodes: Array<Token>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

export type TokenCreationDetails = {
  __typename?: 'TokenCreationDetails';
  createPlt: CreatePlt;
  events: Array<TokenUpdate>;
};

/** An edge in a connection. */
export type TokenEdge = {
  __typename?: 'TokenEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Token;
};

export type TokenEventDetails = BurnEvent | MintEvent | TokenModuleEvent | TokenTransferEvent;

/** A segment of a collection. */
export type TokenEventsCollectionSegment = {
  __typename?: 'TokenEventsCollectionSegment';
  /** A flattened list of the items. */
  items: Array<Cis2Event>;
  /** Information to aid in pagination. */
  pageInfo: CollectionSegmentInfo;
  totalCount: Scalars['Int'];
};

export type TokenHolder = {
  __typename?: 'TokenHolder';
  address: AccountAddress;
};

export type TokenModuleEvent = {
  __typename?: 'TokenModuleEvent';
  details: Scalars['JSON'];
  eventType: Scalars['String'];
};

export type TokenModuleReject = {
  __typename?: 'TokenModuleReject';
  /** The details of the event produced, in the raw byte encoded form. */
  details: Scalars['JSON'];
  /** The type of event produced. */
  reasonType: Scalars['String'];
  /** The unique symbol of the token, which produced this event. */
  tokenId: Scalars['String'];
};

export type TokenTransferEvent = {
  __typename?: 'TokenTransferEvent';
  amount: TokenAmount;
  from: TokenHolder;
  memo?: Maybe<Memo>;
  to: TokenHolder;
};

/** Common event struct for both Holder and Governance events. */
export type TokenUpdate = {
  __typename?: 'TokenUpdate';
  event: TokenEventDetails;
  tokenId: Scalars['String'];
};

export enum TokenUpdateEventType {
  Burn = 'BURN',
  Mint = 'MINT',
  TokenModule = 'TOKEN_MODULE',
  Transfer = 'TRANSFER'
}

export enum TokenUpdateModuleType {
  AddAllowList = 'ADD_ALLOW_LIST',
  AddDenyList = 'ADD_DENY_LIST',
  Pause = 'PAUSE',
  RemoveAllowList = 'REMOVE_ALLOW_LIST',
  RemoveDenyList = 'REMOVE_DENY_LIST',
  Unpause = 'UNPAUSE'
}

/** A segment of a collection. */
export type TokensCollectionSegment = {
  __typename?: 'TokensCollectionSegment';
  /** A flattened list of the items. */
  items: Array<Token>;
  totalCount: Scalars['Int'];
};

export type Transaction = {
  __typename?: 'Transaction';
  block: Block;
  ccdCost: Scalars['UnsignedLong'];
  energyCost: Scalars['Int'];
  /** Transaction index as a string. */
  id: Scalars['ID'];
  result: TransactionResult;
  senderAccountAddress?: Maybe<AccountAddress>;
  transactionHash: Scalars['String'];
  transactionIndex: Scalars['Int'];
  transactionType: TransactionType;
};

export type TransactionConnection = {
  __typename?: 'TransactionConnection';
  /** A list of edges. */
  edges: Array<TransactionEdge>;
  /** A list of nodes. */
  nodes: Array<Transaction>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type TransactionEdge = {
  __typename?: 'TransactionEdge';
  /** A cursor for use in pagination */
  cursor: Scalars['String'];
  /** The item at the end of the edge */
  node: Transaction;
};

export type TransactionFeeCommissionNotInRange = {
  __typename?: 'TransactionFeeCommissionNotInRange';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type TransactionFeeDistributionChainUpdatePayload = {
  __typename?: 'TransactionFeeDistributionChainUpdatePayload';
  baker: Scalars['Decimal'];
  gasAccount: Scalars['Decimal'];
};

export type TransactionMResponse = {
  __typename?: 'TransactionMResponse';
  amount: Scalars['Float'];
  assetName: Scalars['String'];
  dateTime: Scalars['DateTime'];
  from: Scalars['String'];
  to: Scalars['String'];
  transactionHash: Scalars['String'];
  value: Scalars['Float'];
};

export type TransactionMetrics = {
  __typename?: 'TransactionMetrics';
  buckets: TransactionMetricsBuckets;
  /** Total number of transactions (all time). */
  lastCumulativeTransactionCount: Scalars['Int'];
  /** Total number of transactions in the requested period. */
  transactionCount: Scalars['Int'];
};

export type TransactionMetricsBuckets = {
  __typename?: 'TransactionMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /**
   * Total number of transactions (all time) at the end of the bucket period.
   * Intended y-axis value.
   */
  y_LastCumulativeTransactionCount: Array<Scalars['Int']>;
  /**
   * Total number of transactions within the bucket time period. Intended
   * y-axis value.
   */
  y_TransactionCount: Array<Scalars['Int']>;
};

export type TransactionRejectReason = AlreadyABaker | AlreadyADelegator | AmountTooLarge | BakerInCooldown | BakingRewardCommissionNotInRange | CredentialHolderDidNotSign | DelegationTargetNotABaker | DelegatorInCooldown | DuplicateAggregationKey | DuplicateCredIds | EncryptedAmountSelfTransfer | FinalizationRewardCommissionNotInRange | FirstScheduledReleaseExpired | InsufficientBalanceForBakerStake | InsufficientBalanceForDelegationStake | InsufficientDelegationStake | InvalidAccountReference | InvalidAccountThreshold | InvalidContractAddress | InvalidCredentialKeySignThreshold | InvalidCredentials | InvalidEncryptedAmountTransferProof | InvalidIndexOnEncryptedTransfer | InvalidInitMethod | InvalidModuleReference | InvalidProof | InvalidReceiveMethod | InvalidTransferToPublicProof | KeyIndexAlreadyInUse | MissingBakerAddParameters | MissingDelegationAddParameters | ModuleHashAlreadyExists | ModuleNotWf | NonExistentCredIds | NonExistentCredentialId | NonExistentRewardAccount | NonExistentTokenId | NonIncreasingSchedule | NotABaker | NotADelegator | NotAllowedMultipleCredentials | NotAllowedToHandleEncrypted | NotAllowedToReceiveEncrypted | OutOfEnergy | PoolClosed | PoolWouldBecomeOverDelegated | RejectedInit | RejectedReceive | RemoveFirstCredential | RuntimeFailure | ScheduledSelfTransfer | SerializationFailure | StakeOverMaximumThresholdForPool | StakeUnderMinimumThresholdForBaking | TokenModuleReject | TransactionFeeCommissionNotInRange | UnauthorizedTokenGovernance | ZeroScheduledAmount;

export type TransactionResult = Rejected | Success;

export type TransactionType = AccountTransaction | CredentialDeploymentTransaction | UpdateTransaction;

export type Transfer = {
  __typename?: 'Transfer';
  amount: Scalars['Float'];
  assetName: Scalars['String'];
  dateTime: Scalars['DateTime'];
  from: AccountAddress;
  to: AccountAddress;
};

export type TransferMemo = {
  __typename?: 'TransferMemo';
  decoded: DecodedText;
  rawHex: Scalars['String'];
};

export type TransferSummary = {
  __typename?: 'TransferSummary';
  dateTime: Scalars['DateTime'];
  totalAmount: Scalars['Float'];
  transactionCount: Scalars['Int'];
};

export type TransferSummaryResponse = {
  __typename?: 'TransferSummaryResponse';
  dailySummary: Array<TransferSummary>;
  totalTxnCount: Scalars['Int'];
  totalValue: Scalars['Float'];
};

export type Transferred = {
  __typename?: 'Transferred';
  amount: Scalars['UnsignedLong'];
  from: Address;
  to: Address;
};

export type TransferredWithSchedule = {
  __typename?: 'TransferredWithSchedule';
  amountsSchedule: TimestampedAmountConnection;
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

export type UnauthorizedTokenGovernance = {
  __typename?: 'UnauthorizedTokenGovernance';
  /** The unique symbol of the token, which produced this event. */
  tokenId: Scalars['String'];
};

export type UpdateTransaction = {
  __typename?: 'UpdateTransaction';
  updateTransactionType: UpdateTransactionType;
};

export enum UpdateTransactionType {
  BlockEnergyLimitUpdate = 'BLOCK_ENERGY_LIMIT_UPDATE',
  CreatePltUpdate = 'CREATE_PLT_UPDATE',
  FinalizationCommitteeParametersUpdate = 'FINALIZATION_COMMITTEE_PARAMETERS_UPDATE',
  GasRewardsCpv_2Update = 'GAS_REWARDS_CPV_2_UPDATE',
  MintDistributionCpv_1Update = 'MINT_DISTRIBUTION_CPV_1_UPDATE',
  MinBlockTimeUpdate = 'MIN_BLOCK_TIME_UPDATE',
  TimeoutParametersUpdate = 'TIMEOUT_PARAMETERS_UPDATE',
  UpdateAddAnonymityRevoker = 'UPDATE_ADD_ANONYMITY_REVOKER',
  UpdateAddIdentityProvider = 'UPDATE_ADD_IDENTITY_PROVIDER',
  UpdateBakerStakeThreshold = 'UPDATE_BAKER_STAKE_THRESHOLD',
  UpdateCooldownParameters = 'UPDATE_COOLDOWN_PARAMETERS',
  UpdateElectionDifficulty = 'UPDATE_ELECTION_DIFFICULTY',
  UpdateEuroPerEnergy = 'UPDATE_EURO_PER_ENERGY',
  UpdateFoundationAccount = 'UPDATE_FOUNDATION_ACCOUNT',
  UpdateGasRewards = 'UPDATE_GAS_REWARDS',
  UpdateLevel_1Keys = 'UPDATE_LEVEL_1_KEYS',
  UpdateLevel_2Keys = 'UPDATE_LEVEL_2_KEYS',
  UpdateMicroGtuPerEuro = 'UPDATE_MICRO_GTU_PER_EURO',
  UpdateMintDistribution = 'UPDATE_MINT_DISTRIBUTION',
  UpdatePoolParameters = 'UPDATE_POOL_PARAMETERS',
  UpdateProtocol = 'UPDATE_PROTOCOL',
  UpdateRootKeys = 'UPDATE_ROOT_KEYS',
  UpdateTimeParameters = 'UPDATE_TIME_PARAMETERS',
  UpdateTransactionFeeDistribution = 'UPDATE_TRANSACTION_FEE_DISTRIBUTION',
  ValidatorScoreParametersUpdate = 'VALIDATOR_SCORE_PARAMETERS_UPDATE'
}

export type ValidatorPrimedForSuspension = {
  __typename?: 'ValidatorPrimedForSuspension';
  account: AccountAddress;
  bakerId: Scalars['Long'];
};

export type ValidatorScoreParametersUpdate = {
  __typename?: 'ValidatorScoreParametersUpdate';
  maximumMissedRounds: Scalars['UnsignedLong'];
};

export type ValidatorSuspended = {
  __typename?: 'ValidatorSuspended';
  account: AccountAddress;
  bakerId: Scalars['Long'];
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

export type Versions = {
  __typename?: 'Versions';
  apiSupportedDatabaseSchemaVersion: Scalars['String'];
  backendVersion: Scalars['String'];
  databaseSchemaVersion: Scalars['String'];
};

export type ZeroScheduledAmount = {
  __typename?: 'ZeroScheduledAmount';
  /** @deprecated Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};
