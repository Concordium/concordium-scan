import type { PageInfo } from '~/types/generated'

export const PAGE_SIZE = 25

export type PaginationTarget = 'first' | 'previous' | 'next'

/**
 * Hook to control pagination state and actions
 * Returns query variables and a curried navigation handler
 */
export const usePagination = () => {
	const after = ref<PageInfo['endCursor']>(undefined)
	const before = ref<PageInfo['startCursor']>(undefined)
	const first = ref<number | undefined>(PAGE_SIZE)
	const last = ref<number | undefined>(undefined)

	/**
	 * CURRIED: Navigation handler to modify query variables
	 * @param { PageInfo } - Most recent pageInfo
	 * @param { PaginationTarget } - The target (e.g. "next")
	 */
	const goToPage = (pageInfo: PageInfo) => (target: PaginationTarget) => {
		after.value = undefined
		before.value = undefined
		first.value = undefined
		last.value = undefined

		if (target === 'first') {
			first.value = PAGE_SIZE
		} else if (target === 'previous' && pageInfo.hasPreviousPage) {
			before.value = pageInfo.startCursor
			last.value = PAGE_SIZE
		} else if (target === 'next' && pageInfo.endCursor) {
			after.value = pageInfo.endCursor
			first.value = PAGE_SIZE
		} else {
			// eslint-disable-next-line no-console
			console.error('Incorrect pagination arguments:', { target, ...pageInfo })
		}
	}

	return {
		after,
		before,
		first,
		last,
		goToPage,
	}
}
