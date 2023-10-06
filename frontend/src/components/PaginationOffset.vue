<template>
	<div class="pagination-container">
		<div style="display: flex; justify-content: flex-start;">
            Total pages: {{ totalPages }}
        </div>
        <div style="display: flex; justify-content: center;">
            <div class="chevron-button">
                <button
                type="button"
                :disabled="isFirstPages"
                :class="{disabled: isFirstPages}"
                @click="onClickFirstPage"
                >
                    <ChevronDoubleLeftCustomIcon class="chevron-icon"/>
                </button>
            </div>
            <div class="chevron-button">
                <button
                type="button"
                :disabled="isFirstPages"
                :class="{disabled: isFirstPages}"
                @click="onClickPreviousPage"
                >
                    <ChevronLeftCustomIcon class="chevron-icon"/>
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
            <div class="chevron-button">
                <button
                    type="button"
                    :disabled="isLastPages"
                    :class="{disabled: isLastPages}"
                    @click="onClickNextPage"
                >
                    <ChevronRightCustomIcon class="chevron-icon"/>
                </button>
            </div>
            <div class="chevron-button">
                <button
                    type="button"
                    :disabled="isLastPages"
                    :class="{disabled: isLastPages}"
                    @click="onClickLastPage"
                >
                    <ChevronDoubleRightCustomIcon class="chevron-icon"/>
                </button>
            </div>            
        </div>
		<div style="display: flex; justify-content: flex-end;">
            <div style="display: grid; grid-template-rows: auto 20px;">
                <div>
                <div style="display: inline-block;">Page search</div>
                <input 
                    :value="inputPage"
                    :max="totalPages"
                    :min="1"
                    type="number"
                    style="color: black; text-align: right; margin-left: 5px; border-radius: 5px;"
                    @input="onInput"
                />                
                </div>
                <div>
                    <div 
                        v-if="pageInputValidation"
                        style="font-size: 0.75rem; text-wrap: wrap; color: red"
                        >
                        {{ pageInputValidation }}
                    </div>
                </div>            
            </div>
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
const inputPage = ref();

watch(currentPage, (newCurrentPage, _ ) => {
    inputPage.value = newCurrentPage;
}, {immediate: true});

const timeoutId = ref();
const pageInputValidation = ref("");
const onInput = (e: Event) => {
    const inputElement = e.target as HTMLInputElement;
    pageInputValidation.value = "";
    if (!inputElement?.value) {
        return;
    }
    const page = parseInt(inputElement.value);
    if (page > totalPages.value || page < 1) {
        pageInputValidation.value = `Page should be at least 0 and at most ${totalPages.value}`;
        return;
    }
    if (timeoutId.value !== undefined) {
        clearTimeout(timeoutId.value);
    }
    inputPage.value = page
    timeoutId.value = setTimeout(() => {
        const next = Math.max(0, inputPage.value - 1) * props.info.take.value;
        props.info.update(next);
    }, 1_000);
}

const pageFrom = computed(() => currentPage.value - Math.floor((currentPage.value - 1) % NAVIGATION_SIZE));
const pageTo = computed(() => Math.min((Math.floor((currentPage.value - 1) / NAVIGATION_SIZE) + 1) *  NAVIGATION_SIZE, totalPages.value));

const pages = computed(() => {
    const range = [];
    for (let i = pageFrom.value; i <= pageTo.value; i++) {
        range.push({name: i, isDisabled: i !== currentPage.value})
    }
    return range;
})

const isFirstPages = computed(() => Math.floor((currentPage.value - 1) / NAVIGATION_SIZE) === 0);
const isLastPages = computed(() => Math.floor((currentPage.value - 1) / NAVIGATION_SIZE) === Math.floor((totalPages.value - 1) / NAVIGATION_SIZE));

const onClickFirstPage = () => props.info.update(0);

const onClickPreviousPage = () => {
    const previous = Math.max(0, pageFrom.value - NAVIGATION_SIZE - 1) * props.info.take.value;
    props.info.update(previous);
}

const onClickNextPage = () => {
    const next = pageTo.value * props.info.take.value;
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
/* Chrome, Safari, Edge, Opera */
input::-webkit-outer-spin-button,
input::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}

/* Firefox */
input[type=number] {
  -moz-appearance: textfield;
}

.chevron-button {
    display: flex;
    justify-content: center;
}

.chevron-icon {
    height: 20px;
    width: 20px;
}

.disabled {
    opacity: 0.4;
}

div.pagination-container {
	display: grid;
    margin: 30px 0 10px;
	grid-template-columns: repeat(3, auto);
}

div.flex-container {
    display: flex;
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
