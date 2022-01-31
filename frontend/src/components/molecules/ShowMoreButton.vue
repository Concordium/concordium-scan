<template>
	<div>
		<LinkButton
			v-if="newItemCount > 0 && newItemCount <= MAX_PAGE_SIZE"
			role="button"
			:on-click="refetch"
			:class="btnClasses"
		>
			<ArrowUpIcon :class="iconClasses" />
			{{
				newItemCount === 1
					? 'Show 1 more item'
					: `Show ${newItemCount} more items`
			}}
			<ArrowUpIcon :class="iconClasses" />
		</LinkButton>

		<LinkButton
			v-if="newItemCount > MAX_PAGE_SIZE"
			role="button"
			:on-click="refetch"
			:class="btnClasses"
		>
			<RefreshIcon :class="iconClasses" />
			Refresh to see more than {{ MAX_PAGE_SIZE }} new items
			<RefreshIcon :class="iconClasses" />
		</LinkButton>
	</div>
</template>

<script lang="ts" setup>
import { ArrowUpIcon, RefreshIcon } from '@heroicons/vue/outline/index.js'
import { MAX_PAGE_SIZE } from '../../composables/usePagedData'

type Props = {
	newItemCount: number
	refetch: () => void
}

const btnClasses = 'w-full py-3 rounded-lg transition-colors showMoreButton'
const iconClasses = 'h-4 px-2 inline align-baseline'

defineProps<Props>()
</script>

<style>
.showMoreButton:hover {
	background-color: var(--color-button-bg-ghost-hover);
}
</style>
