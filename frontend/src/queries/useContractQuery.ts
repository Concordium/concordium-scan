import { Ref } from 'vue'
import { CombinedError, gql, useQuery } from '@urql/vue'
import { Contract } from '../types/generated'
import { ComponentState } from '../composables/useComponentState'
import { QueryVariables } from '../types/queryVariables'

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
	... on ContractModuleDeployed {
		moduleRef
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
		$afterEvent: String
		$beforeEvent: String
		$firstEvent: Int
		$lastEvent: Int
		$afterRejectEvent: String
		$beforeRejectEvent: String
		$firstRejectEvent: Int
		$lastRejectEvent: Int
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
			contractName
			creator {
				asString
			}
			contractRejectEvents(after: $afterRejectEvent, before: $beforeRejectEvent, first: $firstRejectEvent, last: $lastRejectEvent) {
				nodes { ${rejectEventsFragment} }
				totalCount
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}				
			}
			contractEvents(after: $afterEvent, before: $beforeEvent, first: $firstEvent, last: $lastEvent) {
				nodes { ${eventsFragment} }
				totalCount
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
	contractAddressIndex: Ref<number>
	contractAddressSubIndex: Ref<number>
	eventsVariables: QueryVariables
	rejectEventsVariables: QueryVariables
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
			firstEvent: eventsVariables.first,
			lastEvent: eventsVariables.last,
			afterEvent: eventsVariables.after,
			beforeEvent: eventsVariables.before,
			firstRejectEvent: rejectEventsVariables.first,
			lastRejectEvent: rejectEventsVariables.last,
			afterRejectEvent: rejectEventsVariables.after,
			beforeRejectEvent: rejectEventsVariables.before,
		},
	})

	const componentState = useComponentState<ContractQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
