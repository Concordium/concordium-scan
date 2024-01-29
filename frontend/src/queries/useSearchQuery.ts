import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import type {
	Contract,
	Account,
	PageInfo,
	Block,
	Transaction,
	Baker,
	NodeStatus,
	ModuleReferenceEvent,
	Token,
} from '~/types/generated'
type SearchResponse = {
	search: {
		modules: { nodes: ModuleReferenceEvent[]; pageInfo: PageInfo }
		contracts: { nodes: Contract[]; pageInfo: PageInfo }
		blocks: { nodes: Block[]; pageInfo: PageInfo }
		transactions: { nodes: Transaction[]; pageInfo: PageInfo }
		accounts: { nodes: Account[]; pageInfo: PageInfo }
		nodeStatuses: { nodes: NodeStatus[]; pageInfo: PageInfo }
		tokens: { nodes: Token[]; pageInfo: PageInfo }
		bakers: {
			nodes: Pick<Baker, 'id' | 'bakerId' | 'account'>[]
			pageInfo: PageInfo
		}
	}
}

const SearchQuery = gql<SearchResponse>`
	query Search($query: String!) {
		search(query: $query) {
			modules(first: 3) {
				nodes {
					blockSlotTime
					moduleReference
				}
				pageInfo {
					hasNextPage
				}
			}
			tokens(first: 3) {
				nodes {
					tokenAddress
					tokenId
					contractIndex
					contractSubIndex
					initialTransaction {
						transactionHash
					}
				}
				pageInfo {
					hasNextPage
				}
			}
			contracts(first: 3) {
				nodes {
					blockSlotTime
					contractAddress
					contractAddressIndex
					contractAddressSubIndex
					creator {
						asString
					}
				}
				pageInfo {
					hasNextPage
				}
			}
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
