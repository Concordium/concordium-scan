import { Ref } from 'vue'
import { CombinedError, gql, useQuery } from '@urql/vue'
import { Contract } from '../types/generated'
import { ComponentState } from '../composables/useComponentState'
import { QueryVariables } from '../types/queryVariables'
import { PaginationOffsetQueryVariables } from '../composables/usePaginationOffset'

const eventsFragment = `
blockSlotTime
transactionHash
event {
	__typename
	... on ContractInitialized {
		contractAddress {
			__typename
			asString
			index
			subIndex
		}
		amount
		moduleRef
		initName
		moduleRef
		version
		eventsAsHex {
			nodes
		}
	}
	... on ContractInterrupted {
		contractAddress {
			__typename
			asString
			index
			subIndex
		}
		eventsAsHex {
			nodes
		}
	}
	... on ContractResumed {
		contractAddress {
			__typename
			asString			
			index
			subIndex
		}
		success
	}
	... on ContractUpdated {
		amount
		receiveName
		messageAsHex
		version
		instigator {
			__typename
			... on AccountAddress {
				asString
			}
			... on ContractAddress {
				asString
				index
				subIndex
			}
		}
		contractAddress {
			__typename
			asString
			index
			subIndex
		}
		eventsAsHex {
			nodes
		}		
	}
	... on ContractCall {
		contractUpdated {
			amount
			receiveName
			messageAsHex
			version
			instigator {
				__typename
				... on AccountAddress {
					asString
				}
				... on ContractAddress {
					asString
					index
					subIndex
				}
			}
			contractAddress {
				__typename
				asString
				index
				subIndex
			}
			eventsAsHex {
				nodes
			}			
		}
	}
	... on ContractUpgraded {
		__typename
		contractAddress {
			__typename
			asString
			index
			subIndex
		}
		fromModule: from
		toModule: to
	}
	... on Transferred {
		amount
		from {
			... on ContractAddress {
				__typename
				asString
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
`

const rejectEventsFragment = `
blockSlotTime
transactionHash
rejectedEvent {
  __typename
	... on RejectedReceive {
		rejectReason
		receiveName
		messageAsHex
		contractAddress {
			index
			subIndex
		}
	}        
}
`

const ContractQuery = gql`
	query (
		$skipEvent: Int
		$takeEvent: Int
		$skipRejectEvent: Int
		$takeRejectEvent: Int
		$contractAddressIndex: UnsignedLong!
		$contractAddressSubIndex: UnsignedLong!
	) {
		contract(
			contractAddressIndex: $contractAddressIndex
			contractAddressSubIndex: $contractAddressSubIndex
		) {
			transactionHash
			contractAddress
			blockSlotTime
			moduleReference
			amount
			creator {
				asString
			}
			contractRejectEvents(skip: $skipRejectEvent, take: $takeRejectEvent) {
				items { ${rejectEventsFragment} }
				totalCount
				pageInfo {
					hasPreviousPage
					hasNextPage
				}				
			}
			contractEvents(skip: $skipEvent, take: $takeEvent) {
				items { ${eventsFragment} }
				totalCount
				pageInfo {
					hasPreviousPage
					hasNextPage
				}
			}
		}
	}
`

type QueryParams = {
	contractAddressIndex: Ref<number>
	contractAddressSubIndex: Ref<number>
	eventsVariables: PaginationOffsetQueryVariables
	rejectEventsVariables: PaginationOffsetQueryVariables
}

type ContractQueryResponse = {
	contract: Contract
}

export const useContractQuery = ({
	contractAddressIndex,
	contractAddressSubIndex,
	eventsVariables,
	rejectEventsVariables,
}: QueryParams): {
	data: Ref<ContractQueryResponse | undefined>
	error: Ref<CombinedError | undefined>
	componentState: Ref<ComponentState>
} => {
	const { data, fetching, error } = useQuery<ContractQueryResponse>({
		query: ContractQuery,
		requestPolicy: 'cache-first',
		variables: {
			contractAddressIndex: contractAddressIndex.value,
			contractAddressSubIndex: contractAddressSubIndex.value,
			skipEvent: eventsVariables.skip,
			takeEvent: eventsVariables.take,
			skipRejectEvent: rejectEventsVariables.skip,
			takeRejectEvent: rejectEventsVariables.take,
		},
	})

	const componentState = useComponentState<ContractQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
