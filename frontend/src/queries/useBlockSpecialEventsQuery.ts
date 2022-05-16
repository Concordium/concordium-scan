import { useQuery, gql } from '@urql/vue'
import type { Block } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'
import { useComponentState } from '~/composables/useComponentState'

type BlockSpecialEventsResponse = {
	block: Pick<Block, 'specialEvents'>
}

type BlockQueryVariables = {
	afterFinalizationRewards?: QueryVariables['after']
	beforeFinalizationRewards?: QueryVariables['before']
	firstFinalizationRewards?: QueryVariables['first']
	lastFinalizationRewards?: QueryVariables['last']
	afterBakingRewards?: QueryVariables['after']
	beforeBakingRewards?: QueryVariables['before']
	firstBakingRewards?: QueryVariables['first']
	lastBakingRewards?: QueryVariables['last']
}

const BlockSpecialEventsQuery = gql<BlockSpecialEventsResponse>`
	query (
		$blockId: ID!
		$afterFinalizationRewards: String
		$beforeFinalizationRewards: String
		$firstFinalizationRewards: Int
		$lastFinalizationRewards: Int
	) {
		block(id: $blockId) {
			specialEvents {
				nodes {
					__typename
					... on MintSpecialEvent {
						bakingReward
						finalizationReward
						platformDevelopmentCharge
					}
					... on FinalizationRewardsSpecialEvent {
						remainder
						finalizationRewards(
							after: $afterFinalizationRewards
							before: $beforeFinalizationRewards
							first: $firstFinalizationRewards
							last: $lastFinalizationRewards
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
					... on BlockRewardsSpecialEvent {
						bakerReward
						transactionFees
						oldGasAccount
						newGasAccount
						foundationCharge
						bakerAccountAddress {
							asString
						}
						foundationAccountAddress {
							asString
						}
					}
					... on BakingRewardsSpecialEvent {
						remainder
						bakingRewards {
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
			}
		}
	}
`

type QueryParams = {
	blockId: Block['id']
	paginationVariables?: BlockQueryVariables
}

export const useBlockSpecialEventsQuery = ({
	blockId,
	paginationVariables,
}: QueryParams) => {
	const { data, fetching, error } = useQuery<
		BlockSpecialEventsResponse | undefined
	>({
		query: BlockSpecialEventsQuery,
		requestPolicy: 'cache-first',
		variables: {
			blockId,
			...paginationVariables,
		},
	})

	const componentState = useComponentState<
		BlockSpecialEventsResponse | undefined
	>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
