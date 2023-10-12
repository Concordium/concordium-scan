import { Ref } from "vue"

export const MIN_PAGE_SIZE = 5;

export type PageDropdownInfo = {
    take: Ref<number>
    update: (take: number) => void
}

export const usePageDropdown = (pageSize: number = MIN_PAGE_SIZE) : PageDropdownInfo => {
    const _take = ref<number>(pageSize);

    const updateTake = (take: number) => {
        _take.value = take;
    }

    return {
        take: _take,
        update: updateTake
    }
}
