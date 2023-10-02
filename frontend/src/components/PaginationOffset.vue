<template>
    <ul class="pagination">
        <li>
            <button
            type="button"
            :disabled="isInFirstPage"
            @click="onClickFirstPage"
            >
                First
            </button>
        </li>
        <li>
            <button
            type="button"
            :disabled="isInFirstPage"
            @click="onClickPreviousPage"
            >
                Previous
            </button>
        </li>
        <!--- Visible Buttons Start -->
        <li v-for="page in pages" :key="page.name">
            <button
                type="button"
                :disabled="page.isDisabled"
                :class="{ active: !page.isDisabled }"
                @click="onClickPage(page.name)"
            > {{ page.name }}</button>
        </li>
        <li>
            <button
                type="button"
                :disabled="isInLastPage"
                @click="onClickNextPage"
            >
                Next
            </button>
        </li>
        <li>
            <button
                type="button"
                :disabled="isInLastPage"
                @click="onClickLastPage"
            >
                Last
            </button>
        </li>
    </ul>
</template>
<script lang="ts" setup>
import { NAVIGATION_SIZE, PaginationOffsetInfo } from '../composables/usePaginationOffset'

type Props = {
    info: PaginationOffsetInfo
    totalCount: number
}

const props = defineProps<Props>();

const totalPages = computed(() => {
    const count = Math.floor(props.totalCount / props.info.take.value)
    return props.totalCount % props.info.take.value === 0 ? count : count + 1;
});
const currentPage = computed(() => {
    return Math.floor(props.info.skip.value / props.info.take.value) + 1
})

const pages = computed(() => {
    const from = Math.max(1, currentPage.value - NAVIGATION_SIZE);
    const to = currentPage.value + NAVIGATION_SIZE;
    const range = [];
    for (let i = from; i <= to; i++) {
        range.push({name: i, isDisabled: i !== currentPage.value})
    }
    return range;
})

const isInFirstPage = computed(() => currentPage.value === 1);
const isInLastPage = computed(() => currentPage.value === totalPages.value);

const onClickFirstPage = () => props.info.update(0);
const onClickPreviousPage = () => {
    const previous = Math.max(0, props.info.skip.value - props.info.take.value);
    props.info.update(previous);
}

const onClickNextPage = () => {
    const next = Math.max(0, props.info.skip.value - props.info.take.value);
    props.info.update(next);
}
const onClickLastPage = () => {
    const count = Math.floor(props.totalCount / props.info.take.value)
    props.info.update(count)
}

// eslint-disable-next-line @typescript-eslint/no-empty-function
const onClickPage = (page: number): void => {};

</script>
<style>
.pagination {
    list-style-type: none;
}

.pagination li {
    display: inline-block;
}

.active {
    background-color: #4AAE9B;
    color: #ffffff;
}
</style>
