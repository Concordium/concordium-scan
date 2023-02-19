import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type {
	Account,
	PageInfo,
	Block,
	Transaction,
	Baker,
	NodeStatus,
	Contract,
} from '~/types/generated'
type SearchResponse = {
	search: {
		blocks: { nodes: Block[]; pageInfo: PageInfo }
		transactions: { nodes: Transaction[]; pageInfo: PageInfo }
		accounts: { nodes: Account[]; pageInfo: PageInfo }
		nodeStatuses: { nodes: NodeStatus[]; pageInfo: PageInfo }
		bakers: {
			nodes: Pick<Baker, 'id' | 'bakerId' | 'account'>[]
			pageInfo: PageInfo
		}
		contracts: { nodes: Contract[]; pageInfo: PageInfo }
	}
}

const SearchQuery = gql<SearchResponse>`
	query Search($query: String!) {
		search(query: $query) {
			blocks(first: 3) {
				nodes {
					id
					blockHash
					blockHeight
					blockSlotTime
					transactionCount
				}
				pageInfo {
					hasNextPage
				}
			}
			transactions(first: 3) {
				nodes {
					id
					transactionHash
					block {
						blockHash
						blockHeight
						blockSlotTime
					}
				}
				pageInfo {
					hasNextPage
				}
			}
			accounts(first: 3) {
				nodes {
					id
					createdAt
					address {
						asString
					}
				}
				pageInfo {
					hasNextPage
				}
			}
			bakers(first: 3) {
				pageInfo {
					hasNextPage
				}
				nodes {
					bakerId
					account {
						address {
							asString
						}
					}
				}
			}
			contracts(first: 3) {
				pageInfo {
					hasNextPage
				}
				nodes {
					contractAddress {
						asString
					}
					moduleRef
					balance
					owner {
						asString
					}
					createdTime
				}
			}
			nodeStatuses(first: 3) {
				nodes {
					id
					nodeId
					uptime
					consensusBakerId
					nodeName
					clientVersion
				}
				pageInfo {
					hasNextPage
				}
			}
		}
	}
`

export const useSearchQuery = (query: Ref<string>, paused = true) => {
	const { data, executeQuery, fetching } = useQuery({
		query: SearchQuery,
		requestPolicy: 'network-only',
		variables: {
			query,
		},
		pause: paused,
	})
	return { data, executeQuery, fetching }
}
