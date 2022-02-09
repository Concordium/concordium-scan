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
  /** The `Long` scalar type represents non-fractional signed whole 64-bit numeric values. Long can represent values between -(2^63) and 2^63 - 1. */
  Long: any;
  /** The `TimeSpan` scalar represents an ISO-8601 compliant duration type. */
  TimeSpan: any;
  /** The UnsignedLong scalar type represents a unsigned 64-bit numeric non-fractional value greater than or equal to 0. */
  UnsignedLong: any;
};

export type Account = {
  __typename?: 'Account';
  address: Scalars['String'];
  createdAt: Scalars['DateTime'];
  id: Scalars['ID'];
};

export type AccountAddress = {
  __typename?: 'AccountAddress';
  address: Scalars['String'];
};

export type AccountCreated = {
  __typename?: 'AccountCreated';
  address: Scalars['String'];
};

export type AccountTransaction = {
  __typename?: 'AccountTransaction';
  accountTransactionType?: Maybe<AccountTransactionType>;
};

export enum AccountTransactionType {
  AddBaker = 'ADD_BAKER',
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

export type Address = AccountAddress | ContractAddress;

export type AlreadyABaker = {
  __typename?: 'AlreadyABaker';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  bakerId: Scalars['UnsignedLong'];
};

export type AmountAddedByDecryption = {
  __typename?: 'AmountAddedByDecryption';
  accountAddress: Scalars['String'];
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

export type BakerAdded = {
  __typename?: 'BakerAdded';
  accountAddress: Scalars['String'];
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
  accountAddress: Scalars['String'];
  aggregationKey: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
  electionKey: Scalars['String'];
  signKey: Scalars['String'];
};

export type BakerRemoved = {
  __typename?: 'BakerRemoved';
  accountAddress: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
};

export type BakerSetRestakeEarnings = {
  __typename?: 'BakerSetRestakeEarnings';
  accountAddress: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
  restakeEarnings: Scalars['Boolean'];
};

export type BakerStakeDecreased = {
  __typename?: 'BakerStakeDecreased';
  accountAddress: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type BakerStakeIncreased = {
  __typename?: 'BakerStakeIncreased';
  accountAddress: Scalars['String'];
  bakerId: Scalars['UnsignedLong'];
  newStakedAmount: Scalars['UnsignedLong'];
};

export type BakingReward = {
  __typename?: 'BakingReward';
  address: Scalars['String'];
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

export type Block = {
  __typename?: 'Block';
  bakerId?: Maybe<Scalars['Int']>;
  blockHash: Scalars['String'];
  blockHeight: Scalars['Int'];
  blockSlotTime: Scalars['DateTime'];
  finalizationSummary?: Maybe<FinalizationSummary>;
  finalized: Scalars['Boolean'];
  id: Scalars['ID'];
  specialEvents: SpecialEvents;
  transactionCount: Scalars['Int'];
  transactions?: Maybe<TransactionsConnection>;
};


export type BlockTransactionsArgs = {
  after?: InputMaybe<Scalars['String']>;
  before?: InputMaybe<Scalars['String']>;
  first?: InputMaybe<Scalars['Int']>;
  last?: InputMaybe<Scalars['Int']>;
};

export type BlockMetrics = {
  __typename?: 'BlockMetrics';
  /** The average block time (slot-time difference between two adjacent blocks) in the requested period. */
  avgBlockTime: Scalars['Float'];
  /** Total number of blocks added in requested period. */
  blocksAdded: Scalars['Int'];
  buckets: BlockMetricsBuckets;
  /** The most recent block height (equals the total length of the chain). */
  lastBlockHeight: Scalars['Long'];
  /** The total amount of CCD in encrypted balances. */
  lastTotalEncryptedMicroCcd: Scalars['Long'];
  /** The total amount of CCD in existence. */
  lastTotalMicroCcd: Scalars['Long'];
};

export type BlockMetricsBuckets = {
  __typename?: 'BlockMetricsBuckets';
  /** The width (time interval) of each bucket. */
  bucketWidth: Scalars['TimeSpan'];
  /** Start of the bucket time period. Intended x-axis value. */
  x_Time: Array<Scalars['DateTime']>;
  /** The average block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. */
  y_BlockTimeAvg: Array<Scalars['Float']>;
  /** The maximum block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. */
  y_BlockTimeMax: Array<Scalars['Float']>;
  /** The minimum block time (slot-time difference between two adjacent blocks) in the bucket period. Intended y-axis value. */
  y_BlockTimeMin: Array<Scalars['Float']>;
  /** Number of blocks added within the bucket time period. Intended y-axis value. */
  y_BlocksAdded: Array<Scalars['Int']>;
  /** The total amount of CCD in encrypted balances at the end of the bucket period. Intended y-axis value. */
  y_LastTotalEncryptedMicroCcd: Array<Scalars['Long']>;
  /** The total amount of CCD in existence at the end of the bucket period. Intended y-axis value. */
  y_LastTotalMicroCcd: Array<Scalars['Long']>;
  /** The maximum amount of CCD in encrypted balances in the bucket period. Intended y-axis value. */
  y_MaxTotalEncryptedMicroCcd: Array<Scalars['Long']>;
  /** The minimum amount of CCD in encrypted balances in the bucket period. Intended y-axis value. */
  y_MinTotalEncryptedMicroCcd: Array<Scalars['Long']>;
};

export type BlockRewards = {
  __typename?: 'BlockRewards';
  bakerAccountAddress: Scalars['String'];
  bakerReward: Scalars['UnsignedLong'];
  foundationAccountAddress: Scalars['String'];
  foundationCharge: Scalars['UnsignedLong'];
  newGasAccount: Scalars['UnsignedLong'];
  oldGasAccount: Scalars['UnsignedLong'];
  transactionFees: Scalars['UnsignedLong'];
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

export type ChainUpdateEnqueued = {
  __typename?: 'ChainUpdateEnqueued';
  effectiveTime: Scalars['DateTime'];
};

export type ContractAddress = {
  __typename?: 'ContractAddress';
  index: Scalars['UnsignedLong'];
  subIndex: Scalars['UnsignedLong'];
};

export type ContractInitialized = {
  __typename?: 'ContractInitialized';
  address: ContractAddress;
  amount: Scalars['UnsignedLong'];
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
  address: ContractAddress;
  amount: Scalars['UnsignedLong'];
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
  accountAddress: Scalars['String'];
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
  accountAddress: Scalars['String'];
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

export type EncryptedAmountSelfTransfer = {
  __typename?: 'EncryptedAmountSelfTransfer';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: Scalars['String'];
};

export type EncryptedAmountsRemoved = {
  __typename?: 'EncryptedAmountsRemoved';
  accountAddress: Scalars['String'];
  inputAmount: Scalars['String'];
  newEncryptedAmount: Scalars['String'];
  upToIndex: Scalars['UnsignedLong'];
};

export type EncryptedSelfAmountAdded = {
  __typename?: 'EncryptedSelfAmountAdded';
  accountAddress: Scalars['String'];
  amount: Scalars['UnsignedLong'];
  newEncryptedAmount: Scalars['String'];
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

export type FinalizationReward = {
  __typename?: 'FinalizationReward';
  address: Scalars['String'];
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

export type InsufficientBalanceForBakerStake = {
  __typename?: 'InsufficientBalanceForBakerStake';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type InvalidAccountReference = {
  __typename?: 'InvalidAccountReference';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: Scalars['String'];
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

export enum MetricsPeriod {
  Last7Days = 'LAST7_DAYS',
  Last24Hours = 'LAST24_HOURS',
  Last30Days = 'LAST30_DAYS',
  LastHour = 'LAST_HOUR',
  LastYear = 'LAST_YEAR'
}

export type Mint = {
  __typename?: 'Mint';
  bakingReward: Scalars['UnsignedLong'];
  finalizationReward: Scalars['UnsignedLong'];
  foundationAccount: Scalars['String'];
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
  accountAddress: Scalars['String'];
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
  accountAddress: Scalars['String'];
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
  accountAddress: Scalars['String'];
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

export type Query = {
  __typename?: 'Query';
  account?: Maybe<Account>;
  accountByAddress?: Maybe<Account>;
  accountsMetrics?: Maybe<AccountsMetrics>;
  block?: Maybe<Block>;
  blockByBlockHash?: Maybe<Block>;
  blockMetrics?: Maybe<BlockMetrics>;
  blocks?: Maybe<BlocksConnection>;
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


export type QueryAccountsMetricsArgs = {
  period: MetricsPeriod;
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

export type RuntimeFailure = {
  __typename?: 'RuntimeFailure';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type ScheduledSelfTransfer = {
  __typename?: 'ScheduledSelfTransfer';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
  accountAddress: Scalars['String'];
};

export type SearchResult = {
  __typename?: 'SearchResult';
  accounts: Array<Account>;
  blocks: Array<Block>;
  transactions: Array<Transaction>;
};

export type SerializationFailure = {
  __typename?: 'SerializationFailure';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};

export type SpecialEvents = {
  __typename?: 'SpecialEvents';
  bakingRewards?: Maybe<BakingRewards>;
  blockRewards?: Maybe<BlockRewards>;
  finalizationRewards?: Maybe<FinalizationRewards>;
  mint?: Maybe<Mint>;
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
  senderAccountAddress?: Maybe<Scalars['String']>;
  transactionHash: Scalars['String'];
  transactionIndex: Scalars['Int'];
  transactionType: TransactionType;
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
  fromAccountAddress: Scalars['String'];
  toAccountAddress: Scalars['String'];
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
  UpdateElectionDifficulty = 'UPDATE_ELECTION_DIFFICULTY',
  UpdateEuroPerEnergy = 'UPDATE_EURO_PER_ENERGY',
  UpdateFoundationAccount = 'UPDATE_FOUNDATION_ACCOUNT',
  UpdateGasRewards = 'UPDATE_GAS_REWARDS',
  UpdateLevel1Keys = 'UPDATE_LEVEL1_KEYS',
  UpdateLevel2Keys = 'UPDATE_LEVEL2_KEYS',
  UpdateMicroGtuPerEuro = 'UPDATE_MICRO_GTU_PER_EURO',
  UpdateMintDistribution = 'UPDATE_MINT_DISTRIBUTION',
  UpdateProtocol = 'UPDATE_PROTOCOL',
  UpdateRootKeys = 'UPDATE_ROOT_KEYS',
  UpdateTransactionFeeDistribution = 'UPDATE_TRANSACTION_FEE_DISTRIBUTION'
}

export type ZeroScheduledAmount = {
  __typename?: 'ZeroScheduledAmount';
  /** @deprecated Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields) */
  _: Scalars['Boolean'];
};
