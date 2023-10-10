<template>
	<div class="pagination-container">
		<div style="display: flex; justify-content: flex-start;">
            Total pages: {{ totalPages }}
        </div>
        <div style="display: flex; justify-content: center;">
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
                    :disabled="currentPage === totalPages"
                    :class="{disabled: currentPage === totalPages}"
                    @click="onClickLastPage"
                >
                    <ChevronDoubleRightCustomIcon class="chevron-icon"/>
                </button>
            </div>            
        </div>
		<div style="display: flex; justify-content: flex-end; align-items: center;">
            <form novalidate="true" @submit.prevent="onSubmitInput">
                <label for="inputPage" style="display: inline-block;">Page</label>
                <input
                  id="inputPage"
                  v-model="inputPage"
                  :max="totalPages"
                  :min="1"
                  type="number"
                  style="color: black; text-align: center; margin-left: 5px; border-radius: 5px;"
                />
                <Validation 
                    :text="`Page should be at least 0 and at most ${totalPages}`"
                    :is-visible="isVisible">
                  <button 
                    type="submit"
                    class="click-btn"
                    style="margin-left: 5px;"
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
	display: flex;
    margin: 30px 0 10px;
    justify-content: space-between;
    flex-wrap: wrap;
	/* grid-template-columns: repeat(3, auto); */
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
    padding: 5px;

}

div.button-container button {
    padding: 3px 10px 0;
}

div > button:hover {
    opacity: 0.4;
}

.active {
    background-color: hsl(247, 40%, 18%);
    border-radius: 25px;

    &:hover {
        opacity: 1;
        cursor: default;
    }
}
</style>
