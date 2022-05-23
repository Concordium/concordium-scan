import { useQuery, gql } from '@urql/vue'
import type {
	Block,
	BakingRewardsSpecialEvent,
	BlockRewardsSpecialEvent,
	FinalizationRewardsSpecialEvent,
	MintSpecialEvent,
	BlockAccrueRewardSpecialEvent,
	PaydayAccountRewardSpecialEvent,
	PaydayFoundationRewardSpecialEvent,
	PaydayPoolRewardSpecialEvent,
	PageInfo,
} from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'
import { useComponentState } from '~/composables/useComponentState'

export type FilteredSpecialEvent<SpecialEvent> = {
	nodes: SpecialEvent[]
	pageInfo: PageInfo
}

type BlockSpecialEventsResponse = {
	block: {
		bakingRewards: FilteredSpecialEvent<BakingRewardsSpecialEvent>
		blockRewards: FilteredSpecialEvent<BlockRewardsSpecialEvent>
		finalizationRewards: FilteredSpecialEvent<FinalizationRewardsSpecialEvent>
		mintDistribution: FilteredSpecialEvent<MintSpecialEvent>
		blockAccruedRewards: FilteredSpecialEvent<BlockAccrueRewardSpecialEvent>
		paydayAccountRewards: FilteredSpecialEvent<PaydayAccountRewardSpecialEvent>
		paydayFoundationRewards: FilteredSpecialEvent<PaydayFoundationRewardSpecialEvent>
		paydayPoolRewards: FilteredSpecialEvent<PaydayPoolRewardSpecialEvent>
	}
}

type PaginationGroups =
	| 'blockRewardsPaginationVars'
	| 'bakingRewardsPaginationVars'
	| 'bakingRewardsSubPaginationVars'
	| 'mintDistributionPaginationVars'
	| 'blockAccrueRewardsPaginationVars'
	| 'finalizationRewardsPaginationVars'
	| 'finalizationRewardsSubPaginationVars'
	| 'paydayFoundationRewardsPaginationVars'
	| 'paydayAccountRewardsPaginationVars'
	| 'paydayPoolRewardsPaginationVars'

type BlockQueryVariables = Record<PaginationGroups, QueryVariables>

const BlockSpecialEventsQuery = gql<BlockSpecialEventsResponse>`
	query (
		$blockId: ID!
		$afterBlockRewards: String
		$beforeBlockRewards: String
		$firstBlockRewards: Int
		$lastBlockRewards: Int
		$afterBakingRewards: String
		$beforeBakingRewards: String
		$firstBakingRewards: Int
		$lastBakingRewards: Int
		$afterBakingSubRewards: String
		$beforeBakingSubRewards: String
		$firstBakingSubRewards: Int
		$lastBakingSubRewards: Int
		$afterMintDistribution: String
		$beforeMintDistribution: String
		$firstMintDistribution: Int
		$lastMintDistribution: Int
		$afterBlockAccrueRewards: String
		$beforeBlockAccrueRewards: String
		$firstBlockAccrueRewards: Int
		$lastBlockAccrueRewards: Int
		$afterFinalizationRewards: String
		$beforeFinalizationRewards: String
		$firstFinalizationRewards: Int
		$lastFinalizationRewards: Int
		$afterFinalizationSubRewards: String
		$beforeFinalizationSubRewards: String
		$firstFinalizationSubRewards: Int
		$lastFinalizationSubRewards: Int
		$afterFoundationRewards: String
		$beforeFoundationRewards: String
		$firstFoundationRewards: Int
		$lastFoundationRewards: Int
		$afterAccountRewards: String
		$beforeAccountRewards: String
		$firstAccountRewards: Int
		$lastAccountRewards: Int
		$afterPoolRewards: String
		$beforePoolRewards: String
		$firstPoolRewards: Int
		$lastPoolRewards: Int
	) {
		block(id: $blockId) {
			bakingRewards: specialEvents(
				includeFilter: BAKING_REWARDS
				after: $afterBakingRewards
				before: $beforeBakingRewards
				first: $firstBakingRewards
				last: $lastBakingRewards
			) {
				nodes {
					... on BakingRewardsSpecialEvent {
						id
						bakingRewards(
							after: $afterBakingSubRewards
							before: $beforeBakingSubRewards
							first: $firstBakingSubRewards
							last: $lastBakingSubRewards
						) {
							nodes {
								amount
								accountAddress {
									asString
								}
							}
							pageInfo {
								startCursor
								endCursor
								hasPreviousPage
								hasNextPage
							}
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			blockRewards: specialEvents(
				includeFilter: BLOCK_REWARDS
				after: $afterBlockRewards
				before: $beforeBlockRewards
				first: $firstBlockRewards
				last: $lastBlockRewards
			) {
				nodes {
					... on BlockRewardsSpecialEvent {
						id
						bakerReward
						transactionFees
						oldGasAccount
						newGasAccount
						foundationCharge
						bakerAccountAddress {
							asString
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			finalizationRewards: specialEvents(
				includeFilter: FINALIZATION_REWARDS
				after: $afterFinalizationRewards
				before: $beforeFinalizationRewards
				first: $firstFinalizationRewards
				last: $lastFinalizationRewards
			) {
				nodes {
					... on FinalizationRewardsSpecialEvent {
						finalizationRewards(
							after: $afterFinalizationSubRewards
							before: $beforeFinalizationSubRewards
							first: $firstFinalizationSubRewards
							last: $lastFinalizationSubRewards
						) {
							nodes {
								amount
								accountAddress {
									asString
								}
							}
							pageInfo {
								startCursor
								endCursor
								hasPreviousPage
								hasNextPage
							}
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			mintDistribution: specialEvents(
				includeFilter: MINT
				after: $afterMintDistribution
				before: $beforeMintDistribution
				first: $firstMintDistribution
				last: $lastMintDistribution
			) {
				nodes {
					... on MintSpecialEvent {
						id
						bakingReward
						finalizationReward
						platformDevelopmentCharge
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			blockAccruedRewards: specialEvents(
				includeFilter: BLOCK_ACCRUE_REWARD
				after: $afterBlockAccrueRewards
				before: $beforeBlockAccrueRewards
				first: $firstBlockAccrueRewards
				last: $lastBlockAccrueRewards
			) {
				nodes {
					... on BlockAccrueRewardSpecialEvent {
						id
						bakerId
						bakerReward
						transactionFees
						oldGasAccount
						newGasAccount
						foundationCharge
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			paydayAccountRewards: specialEvents(
				includeFilter: PAYDAY_ACCOUNT_REWARD
				after: $afterAccountRewards
				before: $beforeAccountRewards
				first: $firstAccountRewards
				last: $lastAccountRewards
			) {
				nodes {
					... on PaydayAccountRewardSpecialEvent {
						id
						bakerReward
						finalizationReward
						transactionFees
						account {
							asString
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			paydayFoundationRewards: specialEvents(
				includeFilter: PAYDAY_FOUNDATION_REWARD
				after: $afterFoundationRewards
				before: $beforeFoundationRewards
				first: $firstFoundationRewards
				last: $lastFoundationRewards
			) {
				nodes {
					... on PaydayFoundationRewardSpecialEvent {
						id
						developmentCharge
						foundationAccount {
							asString
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			paydayPoolRewards: specialEvents(
				includeFilter: PAYDAY_POOL_REWARD
				after: $afterPoolRewards
				before: $beforePoolRewards
				first: $firstPoolRewards
				last: $lastPoolRewards
			) {
				nodes {
					... on PaydayPoolRewardSpecialEvent {
						id
						bakerReward
						finalizationReward
						transactionFees
						pool {
							__typename
							... on BakerPoolRewardTarget {
								bakerId
							}
						}
					}
				}
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
		}
	}
`

type QueryParams = {
	blockId: Block['id']
	paginationVariables: BlockQueryVariables
}

export const hasData = (data: Record<string, FilteredSpecialEvent<unknown>>) =>
	Object.keys(data).some((key: string) => data[key] && data[key].nodes?.length)

export const useBlockSpecialEventsQuery = ({
	blockId,
	paginationVariables,
}: QueryParams) => {
	const {
		blockRewardsPaginationVars: {
			first: firstBlockRewards,
			last: lastBlockRewards,
			after: afterBlockRewards,
			before: beforeBlockRewards,
		},
		bakingRewardsPaginationVars: {
			first: firstBakingRewards,
			last: lastBakingRewards,
			after: afterBakingRewards,
			before: beforeBakingRewards,
		},
		bakingRewardsSubPaginationVars: {
			first: firstBakingSubRewards,
			last: lastBakingSubRewards,
			after: afterBakingSubRewards,
			before: beforeBakingSubRewards,
		},
		mintDistributionPaginationVars: {
			first: firstMintDistribution,
			last: lastMintDistribution,
			after: afterMintDistribution,
			before: beforeMintDistribution,
		},
		blockAccrueRewardsPaginationVars: {
			first: firstBlockAccrueRewards,
			last: lastBlockAccrueRewards,
			after: afterBlockAccrueRewards,
			before: beforeBlockAccrueRewards,
		},
		finalizationRewardsPaginationVars: {
			first: firstFinalizationRewards,
			last: lastFinalizationRewards,
			after: afterFinalizationRewards,
			before: beforeFinalizationRewards,
		},
		finalizationRewardsSubPaginationVars: {
			first: firstFinalizationSubRewards,
			last: lastFinalizationSubRewards,
			after: afterFinalizationSubRewards,
			before: beforeFinalizationSubRewards,
		},
		paydayFoundationRewardsPaginationVars: {
			first: firstFoundationRewards,
			last: lastFoundationRewards,
			after: afterFoundationRewards,
			before: beforeFoundationRewards,
		},
		paydayAccountRewardsPaginationVars: {
			first: firstAccountRewards,
			last: lastAccountRewards,
			after: afterAccountRewards,
			before: beforeAccountRewards,
		},
		paydayPoolRewardsPaginationVars: {
			first: firstPoolRewards,
			last: lastPoolRewards,
			after: afterPoolRewards,
			before: beforePoolRewards,
		},
	} = paginationVariables

	const { data, fetching, error } = useQuery<
		BlockSpecialEventsResponse | undefined
	>({
		query: BlockSpecialEventsQuery,
		requestPolicy: 'cache-first',
		variables: {
			blockId,
			firstBakingRewards,
			lastBakingRewards,
			afterBakingRewards,
			beforeBakingRewards,
			firstBakingSubRewards,
			lastBakingSubRewards,
			afterBakingSubRewards,
			beforeBakingSubRewards,
			firstFinalizationRewards,
			lastFinalizationRewards,
			afterFinalizationRewards,
			beforeFinalizationRewards,
			firstFinalizationSubRewards,
			lastFinalizationSubRewards,
			afterFinalizationSubRewards,
			beforeFinalizationSubRewards,
			firstBlockRewards,
			lastBlockRewards,
			afterBlockRewards,
			beforeBlockRewards,
			firstMintDistribution,
			lastMintDistribution,
			afterMintDistribution,
			beforeMintDistribution,
			firstBlockAccrueRewards,
			lastBlockAccrueRewards,
			afterBlockAccrueRewards,
			beforeBlockAccrueRewards,
			firstFoundationRewards,
			lastFoundationRewards,
			afterFoundationRewards,
			beforeFoundationRewards,
			firstAccountRewards,
			lastAccountRewards,
			afterAccountRewards,
			beforeAccountRewards,
			firstPoolRewards,
			lastPoolRewards,
			afterPoolRewards,
			beforePoolRewards,
		},
	})

	const hasDataRef = ref(hasData(data.value?.block ?? {}))

	const componentState = ref(
		useComponentState<boolean | undefined>({
			fetching,
			error,
			data: hasDataRef,
		})
	)

	watch(
		() => data.value,
		value => {
			hasDataRef.value = hasData(value?.block ?? {})
		}
	)

	return { data, error, componentState }
}
