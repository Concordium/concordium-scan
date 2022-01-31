import { Ref } from 'vue'
import type { PageInfo } from '~/types/pageInfo'

const PAGE_SIZE = 25
export const MAX_PAGE_SIZE = 50

/**
 * Hook to control pagination state and actions
 * Returns query variables and a curried navigation handler
 */
export const usePagedData = <PageData>() => {
	const pagedData = ref<PageData[]>([]) as Ref<PageData[]>
	const intention = ref<'fetchNew' | 'loadMore' | 'refresh'>('loadMore')

	const first = ref<number | undefined>(PAGE_SIZE)
	const last = ref<number | undefined>(undefined)
	const after = ref<string | undefined>(undefined)

	// Persist the afterCursor of the last page after fetching new from the top
	const lastAfterCursor = ref<string | undefined>(undefined)

	/**
	 * Fetches the latest n updates (as a side effect)
	 * @param { newItemsCount } - Amount of new items to fetch
	 */
	const fetchNew = (newItems: number) => {
		if (newItems > MAX_PAGE_SIZE) {
			intention.value = 'refresh'
			first.value = PAGE_SIZE
			last.value = undefined
			after.value = undefined
		} else {
			intention.value = 'fetchNew'
			first.value = newItems
			last.value = undefined
			after.value = undefined
		}
	}

	/**
	 * Loads a full new page (as a side effect)
	 */
	const loadMore = () => {
		intention.value = 'loadMore'
		first.value = PAGE_SIZE
		last.value = undefined
		after.value = lastAfterCursor?.value
	}

	const push = (newPage: PageData[]) => [...pagedData.value, ...newPage]
	const unshift = (newPage: PageData[]) => [...newPage, ...pagedData.value]

	const addPagedData = (newPage: PageData[], newPageInfo?: PageInfo) => {
		if (intention.value === 'loadMore') {
			lastAfterCursor.value = newPageInfo?.endCursor
			pagedData.value = push(newPage)
		} else if (intention.value === 'fetchNew') {
			pagedData.value = unshift(newPage)
		} else if (intention.value === 'refresh') {
			lastAfterCursor.value = newPageInfo?.endCursor
			pagedData.value = newPage
		}
	}

	return {
		first,
		last,
		after,
		pagedData,
		addPagedData,
		fetchNew,
		loadMore,
	}
}
