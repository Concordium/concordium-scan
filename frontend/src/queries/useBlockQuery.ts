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
			blockHeight
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

type QueryParams = {
	hash?: Ref<string>
} & {
	eventsVariables?: BlockQueryVariables
}

export const useBlockQuery = ({ hash, eventsVariables }: QueryParams) => {
	const query = BlockQueryByHash
	const identifier = { hash: hash?.value }

	const { data, fetching, error } = useQuery<
		BlockResponse | BlockByBlockHashResponse | undefined
	>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
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
