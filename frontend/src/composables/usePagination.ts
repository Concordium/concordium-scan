import type { PageInfo } from '~/types/pageInfo'

const PAGE_SIZE = 25

export type PaginationTarget = 'first' | 'previous' | 'next'

/**
 * Hook to control pagination state and actions
 * Returns query variables and a curried navigation handler
 */
export const usePagination = () => {
	const afterCursor = ref<string | undefined>(undefined)
	const beforeCursor = ref<string | undefined>(undefined)
	const paginateFirst = ref<number | undefined>(PAGE_SIZE)
	const paginateLast = ref<number | undefined>(undefined)

	/**
	 * CURRIED: Navigation handler to modify query variables
	 * @param { PageInfo } - Most recent pageInfo
	 * @param { PaginationTarget } - The target (e.g. "next")
	 */
	const goToPage = (pageInfo: PageInfo) => (target: PaginationTarget) => {
		afterCursor.value = undefined
		beforeCursor.value = undefined
		paginateLast.value = undefined
		paginateFirst.value = undefined

		if (target === 'first') {
			paginateFirst.value = PAGE_SIZE
		} else if (target === 'previous' && pageInfo.hasPreviousPage) {
			beforeCursor.value = pageInfo.startCursor
			paginateLast.value = PAGE_SIZE
		} else if (target === 'next' && pageInfo.endCursor) {
			afterCursor.value = pageInfo.endCursor
			paginateFirst.value = PAGE_SIZE
		} else {
			// eslint-disable-next-line no-console
			console.error('Incorrect pagination arguments:', { target, ...pageInfo })
		}
	}

	return {
		afterCursor,
		beforeCursor,
		paginateFirst,
		paginateLast,
		goToPage,
	}
}
