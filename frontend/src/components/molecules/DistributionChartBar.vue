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
				:begin-at-zero="true"
				:type="chartType"
				:show-sign="showSign"
				:label-clickable="false"
			/>
		</ClientOnly>
	</MetricCard>
</template>

<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import { computed, defineProps } from 'vue'
import ChartBarST from '../Charts/ChartBarST.vue'

import type { StablecoinResponse } from '~/queries/useStableCoinQuery'

// Define Props
const props = defineProps<{
	stableCoinsData?: StablecoinResponse
	isLoading?: boolean
	labelClickable?: boolean
	showSign?: string
	chartType?: 'supply' | 'uniqueHolders'
}>()

// Computed Properties
const chartLabels = computed(() => {
	const coins = [...(props.stableCoinsData?.liveStablecoins ?? [])]
		.sort((a, b) => (b.totalSupply ?? 0) - (a.totalSupply ?? 0))
		.slice(0, 10)
	return coins.map(item => item.symbol ?? '')
})

const chartData = computed(() => {
	const coins = [...(props.stableCoinsData?.liveStablecoins ?? [])]
		.sort((a, b) => (b.totalSupply ?? 0) - (a.totalSupply ?? 0))
		.slice(0, 10)
	return coins.map(item =>
		props.chartType === 'supply'
			? item.totalSupply ?? null
			: item.totalUniqueHolders ?? null
	)
})
</script>
