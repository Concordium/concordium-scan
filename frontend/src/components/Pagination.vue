<template>
	<nav
		class="flex bottom-0 pagination"
		:class="[position ? position : 'relative', 'justify-center']"
	>
		<Button
			v-if="props.pageInfo.hasPreviousPage"
			class="mr-4 rounded-lg"
			aria-label="Go to the first page"
			:size="size"
			:on-click="goToFirst"
		>
			<ChevronDoubleLeftIcon :class="buttonClasses" />
			<span v-if="size !== 'sm'" class="hidden md:inline">First</span>
		</Button>
		<Button
			v-if="props.pageInfo.hasPreviousPage"
			class="rounded-none rounded-l-lg"
			aria-label="Go to the previous page"
			:size="size"
			group-position="first"
			:on-click="goToPrevious"
		>
			<ChevronRightIcon
				:class="buttonClasses"
				style="transform: rotate(180deg)"
			/>
			<span v-if="size !== 'sm'" class="hidden md:inline">Previous</span>
		</Button>
		<Button
			v-if="props.pageInfo.hasNextPage"
			class="rounded-none rounded-r-lg"
			aria-label="Go to the next page"
			:size="size"
			group-position="last"
			:on-click="goToNext"
		>
			<span v-if="size !== 'sm'" class="hidden md:inline">Next</span>
			<ChevronRightIcon :class="buttonClasses" />
		</Button>
		<Button
			v-if="props.pageInfo.hasNextPage"
			class="ml-4 rounded-lg"
			aria-label="Go to the last page"
			:size="size"
			:on-click="goToLast"
		>
			<ChevronDoubleRightIcon :class="buttonClasses" />
			<span v-if="size !== 'sm'" class="hidden md:inline">Last</span>
		</Button>
	</nav>
</template>

<script lang="ts" setup>
import {
	ChevronRightIcon,
	ChevronDoubleLeftIcon,
	ChevronDoubleRightIcon,
} from '@heroicons/vue/solid/index.js'
import Button from './atoms/Button.vue'
import type { PageInfo } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'

type Props = {
	// would love to use CSSProperties['position'], but seems not to be exported from Vue
	position?: 'relative' | 'sticky'
	size?: 'sm' | 'md'
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()

const buttonClasses =
	props.size === 'sm' ? 'h-4 inline' : 'h-4 inline align-text-top'

const paginate = (target: PaginationTarget) =>
	props.goToPage(props.pageInfo)(target)

const goToFirst = () => paginate('first')
const goToPrevious = () => paginate('previous')
const goToNext = () => paginate('next')
const goToLast = () => paginate('last')
</script>

<style scoped>
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
