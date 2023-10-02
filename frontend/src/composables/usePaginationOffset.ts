import { Ref } from "vue"

export const NAVIGATION_SIZE = 2;

export type PaginationOffsetInfo = {
    skip: Ref<number>
    take: Ref<number>
    update: (skip: number) => void
}

export const usePaginationOffset = (pageSize: number) : PaginationOffsetInfo => {
    const _skip = ref<number>(0);
    const _take = ref<number>(pageSize);

    const updateReferences = (skip: number) => {
        _skip.value = skip;
    }
    
    return {
        skip: _skip,
        take: _take,
        update: updateReferences
    }
}
