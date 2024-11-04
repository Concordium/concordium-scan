import type { Ref } from 'vue'
import type { UseQueryState } from '@urql/vue'

export type ComponentState = 'idle' | 'loading' | 'empty' | 'error' | 'success'

type StateArguments<DataType> = {
	fetching: UseQueryState['fetching']
	error: UseQueryState['error']
	data: Ref<DataType>
}

/**
 * Returns a finite state for a component, derived from a data query status/result
 */
export const useComponentState = <DataType>({
	fetching,
	error,
	data,
}: StateArguments<DataType>): Ref<ComponentState> => {
	const componentState = ref<ComponentState>('idle')

	const deriveState = () => {
		// skip loading state when paginating or refetching
		if (fetching.value && !data.value) {
			componentState.value = 'loading'
		} else if (error.value) {
			componentState.value = 'error'
		} else if (!data.value) {
			componentState.value = 'empty'
		} else {
			componentState.value = 'success'
		}
	}

	watch([() => fetching.value, () => error.value, () => data.value], () =>
		deriveState()
	)

	deriveState()

	return componentState
}
