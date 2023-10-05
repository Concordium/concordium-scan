<template>
    <div class="flex-container">
        <div>
            <button
            type="button"
            :disabled="isInFirstPage"
            @click="onClickFirstPage"
            >
                <ChevronDoubleLeftCustomIcon/>
            </button>
        </div>
        <div>
            <button
            type="button"
            :disabled="isInFirstPage"
            @click="onClickPreviousPage"
            >
                <ChevronLeftCustomIcon/>
            </button>
        </div>
            <div class="flex-container button-container">
            <button
                v-for="page in pages" :key="page.name"
                type="button"
                :class="{ active: !page.isDisabled }"
                @click="onClickPage(page.name)"
            > {{ page.name }}</button>
        </div>
        <div>
            <button
                type="button"
                :disabled="isInLastPage"
                @click="onClickNextPage"
            >
                <ChevronRightCustomIcon/>
            </button>
        </div>
        <div>
            <button
                type="button"
                :disabled="isInLastPage"
                @click="onClickLastPage"
            >
                <ChevronDoubleRightCustomIcon/>
            </button>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { NAVIGATION_SIZE, PaginationOffsetInfo } from '../composables/usePaginationOffset'
import ChevronDoubleLeftCustomIcon from '~/components/icons/ChevronDoubleLeftCustomIcon.vue'
import ChevronLeftCustomIcon from '~/components/icons/ChevronLeftCustomIcon.vue'
import ChevronDoubleRightCustomIcon from '~/components/icons/ChevronDoubleRightCustomIcon.vue'
import ChevronRightCustomIcon from '~/components/icons/ChevronRightCustomIcon.vue'

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
    const to = Math.min(currentPage.value + NAVIGATION_SIZE, totalPages.value);
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
    const next = Math.max(0, props.info.skip.value + props.info.take.value);
    props.info.update(next);
}
const onClickLastPage = () => {
    const remainder = props.totalCount % props.info.take.value;
    const toSkip = remainder === 0 ? 
        props.totalCount - props.info.take.value :
        props.totalCount - remainder;
    props.info.update(toSkip)
}

const onClickPage = (page: number): void => {
    const toSkip = page * props.info.take.value - props.info.take.value;
    props.info.update(toSkip);
};

</script>
<style>
div.flex-container {
    display: flex;
    border: 2px dashed royalblue;
    justify-content: center;
    align-items: center;
}

div.flex-container > div {
    padding: 10px 10px;
}

div.button-container {
    background-color: var(--color-background-elevated);
    border-radius: 25px;
}

div.button-container button {
    padding: 0 10px;
}

div > button:hover {
    background-color: var(--color-background-elevated);
    opacity: 0.4;
    border-radius: 25px;
}

.active {
    background-color: var(--color-background-elevated-hover);
    border-radius: 25px;
}
</style>
