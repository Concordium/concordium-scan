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

const getNavigationSize = (breakpoint: Breakpoint) => {
    switch (true) {
        case breakpoint >= Breakpoint.MD:
            return 10;
        case breakpoint > Breakpoint.XS:
            return 5;
        default:
            return 3;
    }
}

export const useNavigotionSize = () => {
    const { breakpoint } = useBreakpoint();
    const navigationSize = computed(() => getNavigationSize(breakpoint.value));
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
