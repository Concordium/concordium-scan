import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { SuspendedValidators } from '~/types/generated'
import type { QueryVariables } from '~/types/queryVariables'

type SuspendedValidatorsResponse = {
	suspendedValidators: SuspendedValidators
}

export type SuspendedValidatorsType = SuspendedValidators

type SuspendedValidatorQueryVariables = {
	firstSuspendedValidators: QueryVariables['first']
	lastSuspendedValidators: QueryVariables['last']
	afterSuspendedValidators: QueryVariables['after']
	beforeSuspendedValidators: QueryVariables['before']
	firstPrimedForSuspensionValidators: QueryVariables['first']
	lastPrimedForSuspensionValidators: QueryVariables['last']
	afterPrimedForSuspensionValidators: QueryVariables['after']
	beforePrimedForSuspensionValidators: QueryVariables['before']
}

const SuspendedValidatorQuery = gql<SuspendedValidatorsResponse>`
	query (
		$afterSuspendedValidators: String
		$beforeSuspendedValidators: String
		$firstSuspendedValidators: Int
		$lastSuspendedValidators: Int
		$afterPrimedForSuspensionValidators: String
		$beforePrimedForSuspensionValidators: String
		$firstPrimedForSuspensionValidators: Int
		$lastPrimedForSuspensionValidators: Int
	) {
		suspendedValidators {
			suspendedValidators(
				after: $afterSuspendedValidators
				before: $beforeSuspendedValidators
				first: $firstSuspendedValidators
				last: $lastSuspendedValidators
			) {
				nodes {
					id
				}
				pageInfo {
					hasNextPage
					hasPreviousPage
					startCursor
					endCursor
					__typename
				}
				__typename
			}
			primedForSuspensionValidators(
				after: $afterPrimedForSuspensionValidators
				before: $beforePrimedForSuspensionValidators
				first: $firstPrimedForSuspensionValidators
				last: $lastPrimedForSuspensionValidators
			) {
				nodes {
					id
				}
				pageInfo {
					hasNextPage
					hasPreviousPage
					startCursor
					endCursor
					__typename
				}
				__typename
			}

			__typename
		}
	}
`

export const useSuspendedValidatorsQuery = (
	pagingVariables: SuspendedValidatorQueryVariables
) => {
	const { data, fetching, error } = useQuery({
		query: SuspendedValidatorQuery,
		requestPolicy: 'cache-first',
		variables: {
			...pagingVariables,
		},
	})

	const dataRef = ref(data.value?.suspendedValidators)
	const componentState = useComponentState<SuspendedValidators | undefined>({
		fetching,
		error,
		data: dataRef,
	})
	watch(
		() => data.value,
		value => (dataRef.value = value?.suspendedValidators)
	)
	return { data: dataRef, error, componentState }
}
