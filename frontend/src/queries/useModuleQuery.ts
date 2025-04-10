import type { Ref } from 'vue'
import { type CombinedError, gql, useQuery } from '@urql/vue'
import type { ComponentState } from '../composables/useComponentState'
import type { ModuleReferenceEvent } from '../types/generated'
import type { PaginationOffsetQueryVariables } from '../composables/usePaginationOffset'

const ContractQuery = gql`
	query (
		$moduleReference: String!
		$skipEvent: Int
		$takeEvent: Int
		$skipRejectEvent: Int
		$takeRejectEvent: Int
		$skipLinkedContract: Int
		$takeLinkedContract: Int
	) {
		moduleReferenceEvent(moduleReference: $moduleReference) {
			transactionHash
			moduleReference
			blockSlotTime
			sender {
				asString
			}
			displaySchema
			linkedContracts(skip: $skipLinkedContract, take: $takeLinkedContract) {
				items {
					linkedDateTime
					contractAddress {
						asString
						index
						subIndex
					}
				}
				totalCount
			}
			moduleReferenceRejectEvents(
				skip: $skipRejectEvent
				take: $takeRejectEvent
			) {
				items {
					blockSlotTime
					transactionHash
					rejectedEvent {
						__typename
						... on InvalidInitMethod {
							moduleRef
							initName
						}
						... on InvalidReceiveMethod {
							moduleRef
							receiveName
						}
						... on ModuleHashAlreadyExists {
							moduleRef
						}
					}
				}
				totalCount
			}
			moduleReferenceContractLinkEvents(skip: $skipEvent, take: $takeEvent) {
				items {
					blockSlotTime
					transactionHash
					linkAction
					contractAddress {
						asString
						index
						subIndex
					}
				}
				totalCount
			}
		}
	}
`

type QueryParams = {
	moduleReference: Ref<string>
	eventsVariables: PaginationOffsetQueryVariables
	rejectEventsVariables: PaginationOffsetQueryVariables
	linkedContract: PaginationOffsetQueryVariables
}

type ModuleReferenceEventResponse = {
	moduleReferenceEvent: ModuleReferenceEvent
}

export const useModuleReferenceEventQuery = ({
	moduleReference,
	eventsVariables,
	rejectEventsVariables,
	linkedContract,
}: QueryParams): {
	data: Ref<ModuleReferenceEventResponse | undefined>
	error: Ref<CombinedError | undefined>
	componentState: Ref<ComponentState | undefined>
	fetching: Ref<boolean>
} => {
	const { data, fetching, error } = useQuery<ModuleReferenceEventResponse>({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: ContractQuery,
		requestPolicy: 'cache-first',
		variables: {
			moduleReference: moduleReference.value,
			skipEvent: eventsVariables.skip,
			takeEvent: eventsVariables.take,
			skipRejectEvent: rejectEventsVariables.skip,
			takeRejectEvent: rejectEventsVariables.take,
			skipLinkedContract: linkedContract.skip,
			takeLinkedContract: linkedContract.take,
		},
	})

	const componentState = useComponentState<
		ModuleReferenceEventResponse | undefined
	>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState, fetching }
}
