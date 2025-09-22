<template>
	<MetricCard class="w-96 lg:w-full">
		<header class="flex flex-col items-center">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight" />
			</div>

			<div class="text-sm text-theme-faded pt-4 w-72 text-center">
				<slot name="title" />
			</div>
		</header>
		<ClientOnly>
			<ChartBarST
				class="h-28 w-full"
				:y-values="chartData"
				:x-values="chartLabels"
				:tooltip-titles="tokenNames"
				:begin-at-zero="true"
				:type="chartType"
				:show-sign="showSign"
				:label-clickable="true"
			/>
		</ClientOnly>
	</MetricCard>
</template>

<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import { computed, defineProps } from 'vue'
import ChartBarST from '../Charts/ChartBarST.vue'

import type { PltToken } from '~/types/generated'

// Define Props
const props = defineProps<{
	stableCoinsData?: PltToken[]
	isLoading?: boolean
	labelClickable?: boolean
	showSign?: string
	chartType?: 'supply' | 'uniqueHolders'
}>()

const MAX_ITEMS = 10

// Computed Properties
const chartLabels = computed(() => {
	const coins = props.stableCoinsData ?? []
	const labels = coins.slice(0, MAX_ITEMS).map(item => item.tokenId ?? '')

	// Add "Others" if there are more items
	if (coins.length > MAX_ITEMS) {
		labels.push('Others')
	}

	return labels
})

const tokenNames = computed(() => {
	const coins = props.stableCoinsData ?? []
	const names = coins.slice(0, MAX_ITEMS).map(item => item.name ?? '')

	// Add "Others" if there are more items
	if (coins.length > MAX_ITEMS) {
		names.push(`Others (${coins.length - MAX_ITEMS} tokens)`)
	}

	return names
})

const chartData = computed(() => {
	const coins = props.stableCoinsData ?? []
	const mainData = coins
		.slice(0, MAX_ITEMS)
		.map(item =>
			props.chartType === 'supply'
				? item.totalSupply != null && item.decimal != null
					? item.totalSupply / Math.pow(10, item.decimal)
					: null
				: item.totalUniqueHolders != null
				? item.totalUniqueHolders
				: null
		)

	// Calculate cumulative value for remaining items
	if (coins.length > MAX_ITEMS) {
		const othersTotal = coins.slice(MAX_ITEMS).reduce((sum, item) => {
			const value =
				props.chartType === 'supply'
					? item.totalSupply != null && item.decimal != null
						? item.totalSupply / Math.pow(10, item.decimal)
						: 0
					: item.totalUniqueHolders != null
					? item.totalUniqueHolders
					: 0
			return sum + (value ?? 0)
		}, 0)

		mainData.push(othersTotal)
	}

	return mainData
})
</script>
