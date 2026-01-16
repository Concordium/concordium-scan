import { ref, isRef, unref, type Ref } from 'vue'
import type { PageInfo } from '~/types/generated'

const PAGE_SIZE = 25
export const MAX_PAGE_SIZE = 50

/**
 * Hook to control pagination state and actions
 * Returns query variables and a curried navigation handler
 */
export const usePagedData = <PageData>(
	initialData: PageData[] = [],
	pageSize: number | Ref<number> = PAGE_SIZE,
	maxPageSize: number | Ref<number> = MAX_PAGE_SIZE
) => {
	const pagedData = ref<PageData[]>(initialData) as Ref<PageData[]>
	const intention = ref<'fetchNew' | 'loadMore' | 'refresh'>('loadMore')

	// Convert to refs if they aren't already
	const pageSizeRef = isRef(pageSize) ? pageSize : ref(pageSize)
	const maxPageSizeRef = isRef(maxPageSize) ? maxPageSize : ref(maxPageSize)

	const first = ref<number | undefined>(unref(pageSizeRef))
	const last = ref<number | undefined>(undefined)
	const after = ref<PageInfo['endCursor']>(undefined)
	const before = ref<PageInfo['endCursor']>(undefined)

	// Persist the afterCursor of the last page after fetching new from the top
	const lastAfterCursor = ref<PageInfo['startCursor']>(undefined)

	// Persist the top cursor, so we can trigger a new query with the "refresh" action
	const topCursor = ref<PageInfo['startCursor']>(undefined)

	/**
	 * Fetches the latest n updates (as a side effect)
	 * @param { newItemsCount } - Amount of new items to fetch
	 */
	const fetchNew = (newItems: number) => {
		if (newItems > unref(maxPageSizeRef)) {
			intention.value = 'refresh'
			first.value = unref(pageSizeRef)
			last.value = undefined
			after.value = undefined
			before.value = topCursor.value
		} else {
			intention.value = 'fetchNew'
			first.value = undefined
			last.value = newItems
			after.value = undefined
			before.value = topCursor.value
		}
	}

	/**
	 * Loads a full new page (as a side effect)
	 */
	const loadMore = () => {
		intention.value = 'loadMore'
		first.value = unref(pageSizeRef)
		last.value = undefined
		after.value = lastAfterCursor?.value
		before.value = undefined
	}

	const push = (newPage: PageData[]) => [...pagedData.value, ...newPage]
	const unshift = (newPage: PageData[]) => [...newPage, ...pagedData.value]

	const addPagedData = (newPage: PageData[], newPageInfo?: PageInfo) => {
		if (intention.value === 'loadMore') {
			if (!topCursor.value) topCursor.value = newPageInfo?.startCursor
			lastAfterCursor.value = newPageInfo?.endCursor
			pagedData.value = push(newPage)
		} else if (intention.value === 'fetchNew') {
			topCursor.value = newPageInfo?.startCursor
			pagedData.value = unshift(newPage)
		} else if (intention.value === 'refresh') {
			topCursor.value = newPageInfo?.startCursor
			lastAfterCursor.value = newPageInfo?.endCursor
			pagedData.value = newPage
		}
	}

	return {
		first,
		last,
		after,
		before,
		pagedData,
		addPagedData,
		fetchNew,
		loadMore,
	}
}
