<template>
	<nav
		class="flex justify-center mt-8 p-4 sticky bottom-0"
		:class="$style.pagination"
	>
		<Button
			class="mr-4"
			aria-label="Go to the first page"
			:disabled="!props.pageInfo.hasPreviousPage"
			:on-click="goToFirst"
		>
			<ChevronDoubleLeftIcon class="h-4 inline align-text-top" />
			<span class="hidden md:inline">First</span>
		</Button>
		<Button
			class="rounded-none rounded-l-lg"
			aria-label="Go to the previous page"
			:disabled="!props.pageInfo.hasPreviousPage"
			group-position="first"
			:on-click="goToPrevious"
		>
			<ChevronRightIcon
				class="h-4 inline align-text-top"
				style="transform: rotate(180deg)"
			/>
			<span class="hidden md:inline">Previous</span>
		</Button>
		<Button
			class="rounded-none rounded-r-lg"
			aria-label="Go to the next page"
			group-position="last"
			:disabled="!props.pageInfo.hasNextPage"
			:on-click="goToNext"
		>
			<span class="hidden md:inline">Next</span>
			<ChevronRightIcon class="h-4 inline align-text-top" />
		</Button>
	</nav>
</template>

<script lang="ts" setup>
import {
	ChevronRightIcon,
	ChevronDoubleLeftIcon,
} from '@heroicons/vue/solid/index.js'
import Button from './atoms/Button.vue'
import type { PageInfo } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()

const paginate = (target: PaginationTarget) =>
	props.goToPage(props.pageInfo)(target)

const goToFirst = () => paginate('first')
const goToPrevious = () => paginate('previous')
const goToNext = () => paginate('next')
</script>

<style module>
/*
	1. Using after: classes on the element seems not to work as desired
	2. Using @apply will prevent tests from running
*/
.pagination::after {
	content: '';
	position: absolute;
	top: 0;
	z-index: -1;
	width: 100%;
	height: 100%;
	pointer-events: none;
	background: hsl(var(--color-background-primary));
	box-shadow: 0 -20px 50px 10px hsl(247, 40%, 18%);
}
</style>
