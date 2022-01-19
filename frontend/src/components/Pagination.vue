<template>
	<nav class="flex justify-center mt-8">
		<Button
			class="mr-4"
			aria-label="Go to the first page"
			:disabled="!props.pageInfo.hasPreviousPage"
			:on-click="goToFirst"
		>
			<ChevronDoubleLeftIcon class="h-4 inline align-text-top" />
			First
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
			Previous
		</Button>
		<Button
			class="rounded-none rounded-r-lg"
			aria-label="Go to the next page"
			group-position="last"
			:disabled="!props.pageInfo.hasNextPage"
			:on-click="goToNext"
		>
			Next
			<ChevronRightIcon class="h-4 inline align-text-top" />
		</Button>
	</nav>
</template>

<script lang="ts" setup>
import {
	ChevronRightIcon,
	ChevronDoubleLeftIcon,
} from '@heroicons/vue/solid/index.js'
import type { PageInfo } from '~/types/pageInfo'
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
