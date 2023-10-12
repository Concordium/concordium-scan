import { Ref } from "vue"
import { Breakpoint } from "./useBreakpoint";

export type PaginationOffsetQueryVariables = {
    skip: Ref<number|undefined>,
    take: Ref<number|undefined>,
}

export type PaginationOffsetInfo = {
    skip: Ref<number>
    take: Ref<number>
    update: (skip: number) => void
}

const getNavigationSizeFromBreakpoint = (breakpoint: Breakpoint): number => {
    switch (true) {
        case breakpoint > Breakpoint.SM:
            return 10;
        case breakpoint > Breakpoint.XS:
            return 5;
        default:
            return 3;
    }
}

const getNavigationSizeFromCurrentPage = (currentPage: number): number => {
    if (currentPage > 100) {
        return 5;
    }
    return 10;
}

export const useNavigotionSize = (currentPage: Ref<number>): Ref<number> => {
    const { breakpoint } = useBreakpoint();
    const navigationSize = computed(() => {
        const sizeFromCurrentPage = getNavigationSizeFromCurrentPage(currentPage.value);
        const sizeFromBreakpoint = getNavigationSizeFromBreakpoint(breakpoint.value);
        return Math.min(sizeFromBreakpoint, sizeFromCurrentPage);
    });

    return navigationSize;
}

export const usePaginationOffset = (pageSize: Ref<number>) : PaginationOffsetInfo => {
    const _skip = ref<number>(0);
    const _take = pageSize

    const updateReferences = (skip: number) => {
        _skip.value = skip;
    }
    
    return {
        skip: _skip,
        take: _take,
        update: updateReferences
    }
}
