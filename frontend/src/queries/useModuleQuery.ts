import { Ref } from 'vue'
import { CombinedError, gql, useQuery } from '@urql/vue'
import { QueryVariables } from '../types/queryVariables'
import { ComponentState } from '../composables/useComponentState'
import { ModuleReferenceEvent } from '../types/generated'

const ContractQuery = gql`
	query (
		$moduleReference: String!
		$afterEvent: String
		$beforeEvent: String
		$firstEvent: Int
		$lastEvent: Int
		$afterRejectEvent: String
		$beforeRejectEvent: String
		$firstRejectEvent: Int
		$lastRejectEvent: Int
		$afterLinkedContract: String
		$beforeLinkedContract: String
		$firstLinkedContract: Int
		$lastLinkedContract: Int
	) {
		moduleReferenceEvent(moduleReference: $moduleReference) {
			transactionHash
			moduleReference
			blockSlotTime
			sender {
				asString
			}
			linkedContracts(
				after: $afterLinkedContract
				before: $beforeLinkedContract
				first: $firstLinkedContract
				last: $lastLinkedContract
			) {
				nodes {
					linkedDateTime
					contractAddress {
						asString
						index
						subIndex
					}
				}
				totalCount
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			moduleReferenceRejectEvents(
				after: $afterRejectEvent
				before: $beforeRejectEvent
				first: $firstRejectEvent
				last: $lastRejectEvent
			) {
				nodes {
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
				pageInfo {
					startCursor
					endCursor
					hasPreviousPage
					hasNextPage
				}
			}
			moduleReferenceContractLinkEvents(
				after: $afterEvent
				before: $beforeEvent
				first: $firstEvent
				last: $lastEvent
			) {
				nodes {
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
	moduleReference: Ref<string>
	eventsVariables: QueryVariables
	rejectEventsVariables: QueryVariables
	linkedContract: QueryVariables
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
} => {
	const { data, fetching, error } = useQuery<ModuleReferenceEventResponse>({
		query: ContractQuery,
		requestPolicy: 'cache-first',
		variables: {
			moduleReference: moduleReference.value,
			firstEvent: eventsVariables.first,
			lastEvent: eventsVariables.last,
			afterEvent: eventsVariables.after,
			beforeEvent: eventsVariables.before,
			firstRejectEvent: rejectEventsVariables.first,
			lastRejectEvent: rejectEventsVariables.last,
			afterRejectEvent: rejectEventsVariables.after,
			beforeRejectEvent: rejectEventsVariables.before,
			firstLinkedContract: linkedContract.first,
			lastLinkedContract: linkedContract.last,
			afterLinkedContract: linkedContract.after,
			beforeLinkedContract: linkedContract.before,
		},
	})

	const componentState = useComponentState<
		ModuleReferenceEventResponse | undefined
	>({
		fetching,
		error,
		data,
	})

	return { data, error, componentState }
}
