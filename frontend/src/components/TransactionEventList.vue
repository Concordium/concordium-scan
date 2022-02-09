<template>
	<div>
		<ul class="px-4">
			<li
				v-for="(event, i) in events?.nodes"
				:key="i"
				class="border-l-4 py-4 px-6 relative"
				:class="$style.listItem"
			>
				{{ translateTransactionEvents(event) }}
			</li>
		</ul>
		<Pagination
			v-if="events?.pageInfo && events?.totalCount > PAGE_SIZE"
			:page-info="events?.pageInfo"
			:go-to-page="props.goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { translateTransactionEvents } from '~/utils/translateTransactionEvents'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Successful, PageInfo } from '~/types/generated'

type Props = {
	events: Successful['events']
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()
</script>

<style module>
.listItem {
	border-color: hsl(var(--color-primary));
}

/* 1: Half of it's own width, half of the border width */
.listItem::before {
	content: '';
	display: block;
	background: hsl(var(--color-primary));
	height: 1rem;
	width: 1rem;
	position: absolute;
	top: 1rem;
	left: calc(-0.5rem - 2px); /* 1 */
	border-radius: 50%;
}
</style>
