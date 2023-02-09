import { useQuery, gql } from '@urql/vue'
import { Ref } from 'vue'
import { useComponentState } from '~/composables/useComponentState'
import type { Contract } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type ContractQueryVariables = {
	afterTx: QueryVariables['after']
	beforeTx: QueryVariables['before']
	firstTx: QueryVariables['first']
	lastTx: QueryVariables['last']
}

type ContractQueryResponse = {
	contract: Contract
}

const ContractQuery = gql`
	query (
		$address: String!
		$afterTx: String
		$beforeTx: String
		$firstTx: Int
		$lastTx: Int
	) {
		contract(address: $address) {
			id
			contractAddress {
				asString
			}
			owner {
				asString
			}
			transactionsCount
			createdTime
			balance
			moduleRef
			__typename
			transactions(
				after: $afterTx
				before: $beforeTx
				first: $firstTx
				last: $lastTx
			) {
				pageInfo {
					hasNextPage
					hasPreviousPage
					startCursor
					endCursor
					__typename
				}
				nodes {
					__typename
					transaction {
						id
						transactionHash
						senderAccountAddress {
							asString
						}
						ccdCost
						block {
							blockSlotTime
						}
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
				}
			}
		}
	}
`

const getData = (
	responseData: ContractQueryResponse | undefined
): Contract | undefined => {
	if (!responseData) return undefined

	return responseData.contract
}

type QueryParams = {
	address: Ref<string>
} & {
	transactionVariables?: ContractQueryVariables
}

export const useContractQuery = ({
	address,
	transactionVariables,
}: QueryParams) => {
	const query = ContractQuery
	const identifier = { address: address?.value }

	const { data, fetching, error } = useQuery<ContractQueryResponse>({
		query,
		requestPolicy: 'cache-first',
		variables: {
			...identifier,
			...transactionVariables,
		},
	})

	const dataRef = ref(getData(data.value))

	const componentState = useComponentState<Contract | undefined>({
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
