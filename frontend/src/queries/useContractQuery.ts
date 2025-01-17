import type { Ref } from 'vue'
import { type CombinedError, gql, useQuery } from '@urql/vue'
import type { Contract } from '../types/generated'
import type { ComponentState } from '../composables/useComponentState'
import type { PaginationOffsetQueryVariables } from '../composables/usePaginationOffset'

// Uses alias in `ContractUpgraded` since fields `to` and `from`
// since they in this event refers to modules, and in event `Transferred`
// refers to address. Fields with same name but different types are not
// allowed in GraphQL.
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
		events {
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
		events {
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
		message
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
		events {
			nodes
		}
	}
	... on ContractCall {
		contractUpdated {
			amount
			receiveName
			messageAsHex
			message
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
			events {
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
		from
		to
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
		message
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
		$skipTokens: Int
		$takeTokens: Int
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
			snapshot {
				moduleReference
				amount
				contractName
			}
			tokens(skip: $skipTokens, take: $takeTokens) {
				items {
				  tokenAddress
				  tokenId
				  contractIndex
				  contractSubIndex
				  contractAddressFormatted
				  totalSupply
				  metadataUrl
				}
				totalCount
			}
			creator {
				asString
			}
			contractRejectEvents(skip: $skipRejectEvent, take: $takeRejectEvent) {
				items { ${rejectEventsFragment} }
				totalCount
			}
			contractEvents(skip: $skipEvent, take: $takeEvent) {
				items { ${eventsFragment} }
				totalCount
			}
		}
	}
`

type QueryParams = {
	contractAddressIndex: Ref<number>
	contractAddressSubIndex: Ref<number>
	eventsVariables: PaginationOffsetQueryVariables
	rejectEventsVariables: PaginationOffsetQueryVariables
	tokensVariables: PaginationOffsetQueryVariables
}

type ContractQueryResponse = {
	contract: Contract
}

export const useContractQuery = ({
	contractAddressIndex,
	contractAddressSubIndex,
	eventsVariables,
	rejectEventsVariables,
	tokensVariables,
}: QueryParams): {
	data: Ref<ContractQueryResponse | undefined>
	error: Ref<CombinedError | undefined>
	componentState: Ref<ComponentState>
	fetching: Ref<boolean>
} => {
	const { data, fetching, error } = useQuery<ContractQueryResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: ContractQuery,
		requestPolicy: 'cache-first',
		variables: {
			contractAddressIndex: contractAddressIndex.value,
			contractAddressSubIndex: contractAddressSubIndex.value,
			skipEvent: eventsVariables.skip,
			takeEvent: eventsVariables.take,
			skipRejectEvent: rejectEventsVariables.skip,
			takeRejectEvent: rejectEventsVariables.take,
			skipTokens: tokensVariables.skip,
			takeTokens: tokensVariables.take,
		},
	})

	const componentState = useComponentState<ContractQueryResponse | undefined>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState, fetching }
}
