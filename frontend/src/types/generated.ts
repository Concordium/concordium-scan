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
  id: Scalars['ID'];
  releaseSchedule: AccountReleaseSchedule;
  transactionCount: Scalars['Int'];
  transactions?: Maybe<AccountTransactionRelationConnection>;
};


export type AccountAccountStatementArgs = {
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

export enum AccountSort {
  AgeAsc = 'AGE_ASC',
  AgeDesc = 'AGE_DESC',
  AmountAsc = 'AMOUNT_ASC',
  AmountDesc = 'AMOUNT_DESC',
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
  BakingReward = 'BAKING_REWARD',
  BlockReward = 'BLOCK_REWARD',
  FinalizationReward = 'FINALIZATION_REWARD',
  FoundationReward = 'FOUNDATION_REWARD',
  MintReward = 'MINT_REWARD',
  TransactionFee = 'TRANSACTION_FEE',
  TransactionFeeReward = 'TRANSACTION_FEE_REWARD',
  TransferIn = 'TRANSFER_IN',
  TransferOut = 'TRANSFER_OUT'
}

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

export type ActiveBakerState = {
  __typename?: 'ActiveBakerState';
  pendingChange?: Maybe<PendingBakerChange>;
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

export type Baker = {
  __typename?: 'Baker';
  account: Account;
  bakerId: Scalars['Long'];
  id: Scalars['ID'];
  state: BakerState;
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

export type BakerRemoved = {
  __typename?: 'BakerRemoved';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
};

export type BakerSetRestakeEarnings = {
  __typename?: 'BakerSetRestakeEarnings';
  accountAddress: AccountAddress;
  bakerId: Scalars['UnsignedLong'];
  restakeEarnings: Scalars['Boolean'];
};

export enum BakerSort {
  BakerIdAsc = 'BAKER_ID_ASC',
  BakerIdDesc = 'BAKER_ID_DESC',
  StakedAmountAsc = 'STAKED_AMOUNT_ASC',
  StakedAmountDesc = 'STAKED_AMOUNT_DESC'
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

export type BakingReward = {
  __typename?: 'BakingReward';
  address: AccountAddress;
  amount: Scalars['UnsignedLong'];
};

/** A connection to a list of items. */
export type BakingRewardConnection = {
  __typename?: 'BakingRewardConnection';
  /** A list of edges. */
  edges?: Maybe<Array<BakingRewardEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<BakingReward>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type BakingRewardEdge = {
  __typename?: 'BakingRewardEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: BakingReward;
};

export type BakingRewards = {
  __typename?: 'BakingRewards';
  remainder: Scalars['UnsignedLong'];
  rewards?: Maybe<BakingRewardConnection>;
};


export type BakingRewardsRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
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
  /** The total CCD released according to the Concordium promise published on deck.concordium.com. Will be null for blocks with slot time before the published release schedule. */
  totalAmountReleased?: Maybe<Scalars['UnsignedLong']>;
  /** The total CCD staked */
  totalAmountStaked: Scalars['UnsignedLong'];
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
  finalizationSummary?: Maybe<FinalizationSummary>;
  finalized: Scalars['Boolean'];
  id: Scalars['ID'];
  specialEvents?: Maybe<SpecialEventsConnection>;
  specialEventsOld: SpecialEvents;
  transactionCount: Scalars['Int'];
  transactions?: Maybe<TransactionsConnection>;
};


export type BlockSpecialEventsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
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
  /** The amount awarded to the L-Pool. */
  lPoolReward: Scalars['UnsignedLong'];
  /** The new balance of the GAS account. */
  newGasAccount: Scalars['UnsignedLong'];
  /** The old balance of the GAS account. */
  oldGasAccount: Scalars['UnsignedLong'];
  /** The total fees paid for transactions in the block. */
  transactionFees: Scalars['UnsignedLong'];
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
  /** The total amount of CCD in existence. */
  lastTotalMicroCcd: Scalars['Long'];
  /** The total amount of CCD in encrypted balances. */
  lastTotalMicroCcdEncrypted: Scalars['Long'];
  /** The total amount of CCD staked. */
  lastTotalMicroCcdStaked: Scalars['Long'];
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

export type BlockRewards = {
  __typename?: 'BlockRewards';
  bakerAccountAddress: AccountAddress;
  bakerReward: Scalars['UnsignedLong'];
  foundationAccountAddress: AccountAddress;
  foundationCharge: Scalars['UnsignedLong'];
  newGasAccount: Scalars['UnsignedLong'];
  oldGasAccount: Scalars['UnsignedLong'];
  transactionFees: Scalars['UnsignedLong'];
};

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
  __typename?: 'ChainParameters';
  bakerCooldownEpochs: Scalars['UnsignedLong'];
  credentialsPerBlockLimit: Scalars['Int'];
  electionDifficulty: Scalars['Decimal'];
  euroPerEnergy: ExchangeRate;
  foundationAccountAddress: AccountAddress;
  microCcdPerEuro: ExchangeRate;
  minimumThresholdForBaking: Scalars['UnsignedLong'];
  rewardParameters: RewardParameters;
};

export type ChainUpdateEnqueued = {
  __typename?: 'ChainUpdateEnqueued';
  effectiveImmediately: Scalars['Boolean'];
  effectiveTime: Scalars['DateTime'];
  payload: ChainUpdatePayload;
};

export type ChainUpdatePayload = AddAnonymityRevokerChainUpdatePayload | AddIdentityProviderChainUpdatePayload | BakerStakeThresholdChainUpdatePayload | ElectionDifficultyChainUpdatePayload | EuroPerEnergyChainUpdatePayload | FoundationAccountChainUpdatePayload | GasRewardsChainUpdatePayload | Level1KeysChainUpdatePayload | MicroCcdPerEuroChainUpdatePayload | MintDistributionChainUpdatePayload | ProtocolChainUpdatePayload | RootKeysChainUpdatePayload | TransactionFeeDistributionChainUpdatePayload;

export type ContractAddress = {
  __typename?: 'ContractAddress';
  asString: Scalars['String'];
  index: Scalars['UnsignedLong'];
  subIndex: Scalars['UnsignedLong'];
};

export type ContractInitialized = {
  __typename?: 'ContractInitialized';
  amount: Scalars['UnsignedLong'];
  contractAddress: ContractAddress;
  eventsAsHex?: Maybe<StringConnection>;
  initName: Scalars['String'];
  moduleRef: Scalars['String'];
};


export type ContractInitializedEventsAsHexArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type ContractModuleDeployed = {
  __typename?: 'ContractModuleDeployed';
  moduleRef: Scalars['String'];
};

export type ContractUpdated = {
  __typename?: 'ContractUpdated';
  amount: Scalars['UnsignedLong'];
  contractAddress: ContractAddress;
  eventsAsHex?: Maybe<StringConnection>;
  instigator: Address;
  messageAsHex: Scalars['String'];
  receiveName: Scalars['String'];
};


export type ContractUpdatedEventsAsHexArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
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

export enum CredentialDeploymentTransactionType {
  Initial = 'INITIAL',
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
};

export type DecodedTransferMemo = {
  __typename?: 'DecodedTransferMemo';
  decodeType: TextDecodeType;
  text: Scalars['String'];
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

export type Event = AccountCreated | AmountAddedByDecryption | BakerAdded | BakerKeysUpdated | BakerRemoved | BakerSetRestakeEarnings | BakerStakeDecreased | BakerStakeIncreased | ChainUpdateEnqueued | ContractInitialized | ContractModuleDeployed | ContractUpdated | CredentialDeployed | CredentialKeysUpdated | CredentialsUpdated | DataRegistered | EncryptedAmountsRemoved | EncryptedSelfAmountAdded | NewEncryptedAmount | TransferMemo | Transferred | TransferredWithSchedule;

/** A connection to a list of items. */
export type EventsConnection = {
  __typename?: 'EventsConnection';
  /** A list of edges. */
  edges?: Maybe<Array<EventsEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<Event>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
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

export type FinalizationReward = {
  __typename?: 'FinalizationReward';
  address: AccountAddress;
  amount: Scalars['UnsignedLong'];
};

/** A connection to a list of items. */
export type FinalizationRewardConnection = {
  __typename?: 'FinalizationRewardConnection';
  /** A list of edges. */
  edges?: Maybe<Array<FinalizationRewardEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<FinalizationReward>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type FinalizationRewardEdge = {
  __typename?: 'FinalizationRewardEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: FinalizationReward;
};

export type FinalizationRewards = {
  __typename?: 'FinalizationRewards';
  remainder: Scalars['UnsignedLong'];
  rewards?: Maybe<FinalizationRewardConnection>;
};


export type FinalizationRewardsRewardsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
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

export type FinalizationSummary = {
  __typename?: 'FinalizationSummary';
  finalizationDelay: Scalars['Long'];
  finalizationIndex: Scalars['Long'];
  finalizedBlockHash: Scalars['String'];
  finalizers?: Maybe<FinalizersConnection>;
};


export type FinalizationSummaryFinalizersArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type FinalizationSummaryParty = {
  __typename?: 'FinalizationSummaryParty';
  bakerId: Scalars['Long'];
  signed: Scalars['Boolean'];
  weight: Scalars['Long'];
};

/** A connection to a list of items. */
export type FinalizersConnection = {
  __typename?: 'FinalizersConnection';
  /** A list of edges. */
  edges?: Maybe<Array<FinalizersEdge>>;
  /** A flattened list of the nodes. */
  nodes?: Maybe<Array<FinalizationSummaryParty>>;
  /** Information to aid in pagination. */
  pageInfo: PageInfo;
};

/** An edge in a connection. */
export type FinalizersEdge = {
  __typename?: 'FinalizersEdge';
  /** A cursor for use in pagination. */
  cursor: Scalars['String'];
  /** The item at the end of the edge. */
  node: FinalizationSummaryParty;
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

export type InsufficientBalanceForBakerStake = {
  __typename?: 'InsufficientBalanceForBakerStake';
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

export type Mint = {
  __typename?: 'Mint';
  bakingReward: Scalars['UnsignedLong'];
  finalizationReward: Scalars['UnsignedLong'];
  foundationAccountAddress: AccountAddress;
  platformDevelopmentCharge: Scalars['UnsignedLong'];
};

export type MintDistribution = {
  __typename?: 'MintDistribution';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
  mintPerSlot: Scalars['Decimal'];
};

export type MintDistributionChainUpdatePayload = {
  __typename?: 'MintDistributionChainUpdatePayload';
  bakingReward: Scalars['Decimal'];
  finalizationReward: Scalars['Decimal'];
  mintPerSlot: Scalars['Decimal'];
};

export type MintSpecialEvent = {
  __typename?: 'MintSpecialEvent';
  bakingReward: Scalars['UnsignedLong'];
  finalizationReward: Scalars['UnsignedLong'];
  foundationAccountAddress: AccountAddress;
  id: Scalars['ID'];
  platformDevelopmentCharge: Scalars['UnsignedLong'];
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

export type NewEncryptedAmount = {
  __typename?: 'NewEncryptedAmount';
  accountAddress: AccountAddress;
  encryptedAmount: Scalars['String'];
  newIndex: Scalars['UnsignedLong'];
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

export type PaydayPoolRewardSpecialEvent = {
  __typename?: 'PaydayPoolRewardSpecialEvent';
  /** Accrued baking rewards for pool. */
  bakerReward: Scalars['UnsignedLong'];
  /** Accrued finalization rewards for pool. */
  finalizationReward: Scalars['UnsignedLong'];
  id: Scalars['ID'];
  /** The pool owner (L-Pool when null). */
  poolOwner?: Maybe<Scalars['UnsignedLong']>;
  /** Accrued transaction fees for pool. */
  transactionFees: Scalars['UnsignedLong'];
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
  blockMetrics?: Maybe<BlockMetrics>;
  blocks?: Maybe<BlocksConnection>;
  rewardMetrics: RewardMetrics;
  rewardMetricsForBaker: RewardMetrics;
  search: SearchResult;
  transaction?: Maybe<Transaction>;
  transactionByTransactionHash?: Maybe<Transaction>;
  transactionMetrics?: Maybe<TransactionMetrics>;
  transactions?: Maybe<TransactionsConnection>;
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


export type QueryRewardMetricsArgs = {
  period: MetricsPeriod;
};


export type QueryRewardMetricsForBakerArgs = {
  bakerId: Scalars['Long'];
  period: MetricsPeriod;
};


export type QuerySearchArgs = {
  query: Scalars['String'];
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

export type RewardParameters = {
  __typename?: 'RewardParameters';
  gasRewards: GasRewards;
  mintDistribution: MintDistribution;
  transactionFeeDistribution: TransactionFeeDistribution;
};

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

export type SpecialEvents = {
  __typename?: 'SpecialEvents';
  bakingRewards?: Maybe<BakingRewards>;
  blockRewards?: Maybe<BlockRewards>;
  finalizationRewards?: Maybe<FinalizationRewards>;
  mint?: Maybe<Mint>;
};

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
  blockAdded: Block;
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

export type TimestampedAmount = {
  __typename?: 'TimestampedAmount';
  amount: Scalars['UnsignedLong'];
  timestamp: Scalars['DateTime'];
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

export type TransactionRejectReason = AlreadyABaker | AmountTooLarge | BakerInCooldown | CredentialHolderDidNotSign | DuplicateAggregationKey | DuplicateCredIds | EncryptedAmountSelfTransfer | FirstScheduledReleaseExpired | InsufficientBalanceForBakerStake | InvalidAccountReference | InvalidAccountThreshold | InvalidContractAddress | InvalidCredentialKeySignThreshold | InvalidCredentials | InvalidEncryptedAmountTransferProof | InvalidIndexOnEncryptedTransfer | InvalidInitMethod | InvalidModuleReference | InvalidProof | InvalidReceiveMethod | InvalidTransferToPublicProof | KeyIndexAlreadyInUse | ModuleHashAlreadyExists | ModuleNotWf | NonExistentCredIds | NonExistentCredentialId | NonExistentRewardAccount | NonIncreasingSchedule | NotABaker | NotAllowedMultipleCredentials | NotAllowedToHandleEncrypted | NotAllowedToReceiveEncrypted | OutOfEnergy | RejectedInit | RejectedReceive | RemoveFirstCredential | RuntimeFailure | ScheduledSelfTransfer | SerializationFailure | StakeUnderMinimumThresholdForBaking | ZeroScheduledAmount;

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
  decoded: DecodedTransferMemo;
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

export enum UpdateTransactionType {
  UpdateAddAnonymityRevoker = 'UPDATE_ADD_ANONYMITY_REVOKER',
  UpdateAddIdentityProvider = 'UPDATE_ADD_IDENTITY_PROVIDER',
  UpdateBakerStakeThreshold = 'UPDATE_BAKER_STAKE_THRESHOLD',
  UpdateCooldownParameters = 'UPDATE_COOLDOWN_PARAMETERS',
  UpdateElectionDifficulty = 'UPDATE_ELECTION_DIFFICULTY',
  UpdateEuroPerEnergy = 'UPDATE_EURO_PER_ENERGY',
  UpdateFoundationAccount = 'UPDATE_FOUNDATION_ACCOUNT',
  UpdateGasRewards = 'UPDATE_GAS_REWARDS',
  UpdateLevel1Keys = 'UPDATE_LEVEL1_KEYS',
  UpdateLevel2Keys = 'UPDATE_LEVEL2_KEYS',
  UpdateMicroGtuPerEuro = 'UPDATE_MICRO_GTU_PER_EURO',
  UpdateMintDistribution = 'UPDATE_MINT_DISTRIBUTION',
  UpdatePoolParameters = 'UPDATE_POOL_PARAMETERS',
  UpdateProtocol = 'UPDATE_PROTOCOL',
  UpdateRootKeys = 'UPDATE_ROOT_KEYS',
  UpdateTimeParameters = 'UPDATE_TIME_PARAMETERS',
  UpdateTransactionFeeDistribution = 'UPDATE_TRANSACTION_FEE_DISTRIBUTION'
}

export type ZeroScheduledAmount = {
  __typename?: 'ZeroScheduledAmount';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};
