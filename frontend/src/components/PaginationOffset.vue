<template>
	<div class="pagination-container">
		<div>
            {{ `${totalPagesText} ${totalPages}` }}
        </div>
        <div class="navigation-container">
            <div class="chevron-button">
                <button
                type="button"
                :disabled="currentPage === 1"
                :class="{disabled: currentPage === 1}"
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
            <div class="flex-container page-number-btn-container">
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
                    :disabled="currentPage === totalPages"
                    :class="{disabled: currentPage === totalPages}"
                    @click="onClickLastPage"
                >
                    <ChevronDoubleRightCustomIcon class="chevron-icon"/>
                </button>
            </div>            
        </div>
		<div>
            <form novalidate="true" @submit.prevent="onSubmitInput">
                <label for="inputPage" style="display: inline-block;">Page:</label>
                <input
                  id="inputPage"
                  v-model="inputPage"
                  :max="totalPages"
                  :min="1"
                  type="number"
                  class="page-search-input"
                />
                <Validation 
                    :text="`Page should be at least 0 and at most ${totalPages}`"
                    :is-visible="isVisible">
                  <button 
                    type="submit"
                    class="click-btn page-search-btn"
                    >
                        Go
                    </button>
                </Validation>                
            </form>
		</div>
	</div>    
</template>
<script lang="ts" setup>
import { PaginationOffsetInfo, useNavigotionSize } from '../composables/usePaginationOffset'
import { Breakpoint } from '../composables/useBreakpoint'
import Validation from './atoms/Validation.vue'
import ChevronDoubleLeftCustomIcon from '~/components/icons/ChevronDoubleLeftCustomIcon.vue'
import ChevronLeftCustomIcon from '~/components/icons/ChevronLeftCustomIcon.vue'
import ChevronDoubleRightCustomIcon from '~/components/icons/ChevronDoubleRightCustomIcon.vue'
import ChevronRightCustomIcon from '~/components/icons/ChevronRightCustomIcon.vue'

type Props = {
    info: PaginationOffsetInfo
    totalCount: number
}
const props = defineProps<Props>();

const { breakpoint } = useBreakpoint();

// Text computations
const totalPagesText = computed(() => {
    if (breakpoint.value <= Breakpoint.XS) {
        return "Pages: "
    }
    return "Total pages: "
})

// Page computations
const totalPages = computed(() => {
    const count = Math.floor(props.totalCount / props.info.take.value)
    return props.totalCount % props.info.take.value === 0 ? count : count + 1;
});
const currentPage = computed(() => {
    return Math.floor(props.info.skip.value / props.info.take.value) + 1
})

// Page go-to computation and validations
const inputPage = ref();
watch(currentPage, (newCurrentPage, _ ) => {
    inputPage.value = newCurrentPage;
}, {immediate: true});
const isVisible = ref(false);

const onSubmitInput = () => {
    isVisible.value = false;
    if (!inputPage.value) {
        return;
    }
    const page = parseInt(inputPage.value);
    if (page > totalPages.value || page < 1) {
        isVisible.value = true;
        setTimeout(() => {
            isVisible.value = false;
        }, 5_000);
        return;
    }
    const next = Math.max(0, inputPage.value - 1) * props.info.take.value;
    props.info.update(next);
}

// Page navigation computations
const navigationSize = useNavigotionSize();

const pageFrom = computed(() => currentPage.value - Math.floor((currentPage.value - 1) % navigationSize.value));
const pageTo = computed(() => Math.min((Math.floor((currentPage.value - 1) / navigationSize.value) + 1) *  navigationSize.value, totalPages.value));
const pages = computed(() => {
    const range = [];
    for (let i = pageFrom.value; i <= pageTo.value; i++) {
        range.push({name: i, isDisabled: i !== currentPage.value})
    }
    return range;
})
const isFirstPages = computed(() => Math.floor((currentPage.value - 1) / navigationSize.value) === 0);
const isLastPages = computed(() => Math.floor((currentPage.value - 1) / navigationSize.value) === Math.floor((totalPages.value - 1) / navigationSize.value));

// Chevron click handlers
const onClickFirstPage = () => props.info.update(0);
const onClickPreviousPage = () => {
    const previous = Math.max(0, pageFrom.value - navigationSize.value - 1) * props.info.take.value;
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

// Dynamic styling
const minWidth = computed(() => {
    if (pageFrom.value >= 10_000) {
        return "70px"
    }
    if (pageFrom.value >= 1_000) {
        return "60px"
    }
    if (pageFrom.value >= 100) {
        return "50px"
    }
    return "40px";
});

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
    padding: 0 5px;
}

.chevron-icon {
    height: 20px;
    width: 20px;
}

.pagination-container {
    display: grid;
    height: 100px;
    align-items: center;
    grid-template-columns: repeat(4, auto);
    grid-template-rows: repeat(2, auto);

    @media (max-width: 1024px) {
        row-gap: 15px;
        margin: 20px 0 5px 0;
    }
}

.pagination-container > div:nth-child(1) {
        grid-column: 1 / 2;
        grid-row: 1 / 3;
}
.pagination-container > div:nth-child(2) {
    grid-column: 2 / 4;
    grid-row: 1 / 3;
}
.pagination-container > div:nth-child(3) {
    grid-column: 4 / 5;
    grid-row: 1 / 3;
    justify-self: end;
}    
@media (max-width: 1024px) {
    .pagination-container > div:nth-child(1) {
        grid-column: 1 / 3;
        grid-row: 1 / 2;
    }
    .pagination-container > div:nth-child(2) {
        grid-column: 1 / 5;
        grid-row: 2 / 3;
    }
    .pagination-container > div:nth-child(3) {
        grid-column: 3 / 5;
        grid-row: 1 / 2;
        justify-self: end;
    }
}

.navigation-container {
    display: flex;
    justify-content: center;
}

.page-number-btn-container {
    display: flex;
    justify-content: center;
    align-items: center;
    background-color: var(--color-background-elevated);
    border-radius: 25px;
    padding: 5px;
    margin: 0 10px;
}

.page-number-btn-container button {
    padding: 3px 10px 0;
    min-width: v-bind(minWidth);
}

div > button:hover {
    opacity: 0.4;
}

div > button.disabled:hover {
    opacity: 0.1;
    cursor: default;
}

.disabled {
    opacity: 0.1;
}

.active {
    background-color: hsl(247, 40%, 18%);
    border-radius: 25px;

    &:hover {
        opacity: 1;
        cursor: default;
    }
}

.page-search-input {
    color: black;
    text-align: center;
    margin: 0 10px;
    border-radius: 5px;
    height: 37px;
    width: 52px;
}

.page-search-btn {
    height: 37px;
    padding: 0 15px;
}
</style>
