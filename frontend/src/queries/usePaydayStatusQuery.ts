import { useQuery, gql } from '@urql/vue'
import { useComponentState } from '~/composables/useComponentState'
import type { PaydayStatus } from '~/types/generated'

export type PaydayStatusQueryResponse = {
	paydayStatus: PaydayStatus
}

const PaydayStatusQuery = gql<PaydayStatusQueryResponse>`
	query {
		paydayStatus {
			nextPaydayTime
			paydaySummaries {
				nodes {
					block {
						blockHash
						blockSlotTime
					}
				}
			}
		}
	}
`

export const usePaydayStatusQuery = () => {
	const { data, error, fetching } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: PaydayStatusQuery,
		requestPolicy: 'cache-and-network',
	})

	const dataRef = ref(data.value?.paydayStatus)

	const componentState = useComponentState<PaydayStatus | undefined>({
		fetching,
		error,
		data: dataRef,
	})

	watch(
		() => data.value,
		value => (dataRef.value = value?.paydayStatus)
	)

	return { data, error, componentState }
}
