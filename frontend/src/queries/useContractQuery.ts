import { Ref } from 'vue'
import { CombinedError, gql, useQuery } from '@urql/vue'
import { Contract } from '../types/generated'
import { ComponentState } from '../composables/useComponentState'

const ContractQuery = gql`
	query (
		$contractAddressIndex: UnsignedLong!
		$contractAddressSubIndex: UnsignedLong!
	) {
		contract(
			contractAddressIndex: $contractAddressIndex
			contractAddressSubIndex: $contractAddressSubIndex
		) {
			transactionHash
			contractAddress
			creator {
				asString
			}
			amount
			contractEvents {
				event {
					__typename
					... on ContractInitialized {
						contractAddress {
							__typename
							index
							subIndex
						}
						amount
						moduleRef
					}
					... on ContractInterrupted {
						contractAddress {
							__typename
							index
							subIndex
						}
					}
					... on ContractResumed {
						contractAddress {
							__typename
							index
							subIndex
						}
						success
					}
					... on ContractModuleDeployed {
						moduleRef
					}
					... on ContractUpdated {
						instigator {
							__typename
							... on AccountAddress {
								asString
							}
							... on ContractAddress {
								index
								subIndex
							}
						}
						contractAddress {
							__typename
							index
							subIndex
						}
					}
					... on ContractUpgraded {
						__typename
						fromModule: from
						toModule: to
					}
					... on Transferred {
						amount
						from {
							... on ContractAddress {
								__typename
								index
								subIndex
							}
						}
						to {
							... on AccountAddress {
								__typename
								asString
							}
						}
					}
				}
			}
		}
	}
`

type QueryParams = {
	contractAddressIndex: Ref<number>
	contractAddressSubIndex: Ref<number>
}

type ContractQueryResponse = {
	contract: Contract
}

const getData = (
	responseData: ContractQueryResponse | undefined
): Contract | undefined => {
	if (!responseData) return undefined

	return responseData.contract
}

export const useContractQuery = ({
	contractAddressIndex,
	contractAddressSubIndex,
}: QueryParams): {
	data: Ref<ContractQueryResponse | undefined>
	error: Ref<CombinedError | undefined>
	componentState: Ref<ComponentState>
} => {
	const { data, fetching, error } = useQuery<ContractQueryResponse>({
		query: ContractQuery,
		requestPolicy: 'cache-first',
		variables: {
			contractAddressIndex: contractAddressIndex?.value,
			contractAddressSubIndex: contractAddressSubIndex?.value,
		},
	})

	const componentState = useComponentState<ContractQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
