import { useQuery, gql } from '@urql/vue'
import type { Ref } from 'vue'
import type { Block } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'
import { useComponentState } from '~/composables/useComponentState'

type BlockResponse = {
	block: Block
}
type BlockByBlockHashResponse = {
	blockByBlockHash: Block
}

type BlockQueryVariables = {
	afterTx: QueryVariables['after']
	beforeTx: QueryVariables['before']
	firstTx: QueryVariables['first']
	lastTx: QueryVariables['last']
}

const transactionsFragment = `
nodes {
	id
	transactionHash
	senderAccountAddress {
		asString
	}
	ccdCost
	result {
		__typename
	}
	transactionType {
		__typename
		... on AccountTransaction {
			accountTransactionType
		}
		... on CredentialDeploymentTransaction {
			credentialDeploymentTransactionType
		}
		... on UpdateTransaction {
			updateTransactionType
		}
	}
}
pageInfo {
	startCursor
	endCursor
	hasPreviousPage
	hasNextPage
}
`
const BlockQuery = gql<BlockResponse>`
	query ($id: ID!, $afterTx: String, $beforeTx: String, $firstTx: Int, $lastTx: Int) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions(after: $afterTx, before: $beforeTx, first: $firstTx, last: $lastTx) {
				${transactionsFragment}
			}
			blockStatistics {
				blockTime
				finalizationTime
			}
		}
	}
`

const BlockQueryByHash = gql<BlockByBlockHashResponse>`
	query (
		$hash: String!
		$afterTx: String
		$beforeTx: String
		$firstTx: Int
		$lastTx: Int
	) {
		blockByBlockHash(blockHash: $hash) {
			id
			blockHash
			bakerId
			blockSlotTime
			finalized
			transactionCount
			transactions(after: $afterTx, before: $beforeTx, first: $firstTx, last: $lastTx) {
				${transactionsFragment}
			}
			blockStatistics {
				blockTime
				finalizationTime
			}
		}
	}
`

type QueryParams = (
	| {
			id: Ref<string>
			hash?: Ref<string>
	  }
	| {
			hash: Ref<string>
			id?: Ref<string>
	  }
) & {
	eventsVariables?: BlockQueryVariables
}

export const useBlockQuery = ({ id, hash, eventsVariables }: QueryParams) => {
	const query = id?.value ? BlockQuery : BlockQueryByHash
	const identifier = id?.value ? { id: id.value } : { hash: hash?.value }

	const { data, fetching, error } = useQuery<
		BlockResponse | BlockByBlockHashResponse | undefined
	>({
		query,
		requestPolicy: 'cache-first',
		variables: {
			...identifier,
			...eventsVariables,
		},
	})

	const getData = (
		responseData: BlockResponse | BlockByBlockHashResponse | undefined
	): Block | undefined => {
		if (!responseData) return undefined

		return 'block' in responseData
			? responseData.block
			: responseData.blockByBlockHash
	}

	const dataRef = ref(getData(data.value))

	const componentState = useComponentState<Block | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = getData(value))
	)

	return { data: dataRef, error, componentState }
}
