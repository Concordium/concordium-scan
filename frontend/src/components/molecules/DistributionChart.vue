<template>
	<MetricCard class="w-100 lg:w-full">
		<header class="flex flex-col items-center">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight" />
			</div>

			<div class="text-sm text-theme-faded pt-4 w-72 text-center">
				<slot name="title" />
			</div>
		</header>
		<ClientOnly>
			<DoughnutChart
				:data="chartData"
				:labels="chartLabels"
				:background-colors="baseColors"
				:hover-background-colors="hoverColors"
				:label-clickable="true"
			/>
		</ClientOnly>
	</MetricCard>
</template>

<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import { defineProps, computed } from 'vue'

import type { Stablecoin } from '~/queries/useStableCoinQuery'

import DoughnutChart from '../Charts/DoughnutChart.vue'

const baseColors = [
	'#2AE8B8', // Bright Mint
	'#3C8AFF', // Vivid Blue
	'#FFD116', // Gold Yellow
	'#FFB21D', // Rich Amber
	'#4FD1FF', // Aqua Blue
	'#1CC6AE', // Electric Teal
	'#A393FF', // Periwinkle
	'#FF6B6B', // Coral Red
	'#D9D9D9', // Silver Grey
	'#FFA3D7', // Soft Pink
]

const hoverColors = [
	'#2AE8B8', // Bright Mint
	'#3C8AFF', // Vivid Blue
	'#FFD116', // Gold Yellow
	'#FFB21D', // Rich Amber
	'#4FD1FF', // Aqua Blue
	'#1CC6AE', // Electric Teal
	'#A393FF', // Periwinkle
	'#FF6B6B', // Coral Red
	'#D9D9D9', // Silver Grey
	'#FFA3D7', // Soft Pink
]

// Define Props
const props = defineProps<{
	stableCoinsData?: Stablecoin[]
	isLoading?: boolean
	labelClickable?: boolean
}>()

// Computed Properties
const chartLabels = computed(() => {
	const coins = (props.stableCoinsData ? [...props.stableCoinsData] : [])
		.sort((a, b) => (b.totalSupply ?? 0) - (a.totalSupply ?? 0))
		.slice(0, 10)
	return coins.map(item => item.symbol ?? 'undefined')
})
const chartData = computed<number[]>(() => {
	const coins = (props.stableCoinsData ? [...props.stableCoinsData] : [])
		.sort((a, b) => (b.totalSupply ?? 0) - (a.totalSupply ?? 0))
		.slice(0, 10)
	return coins.map(item => {
		const value = Number(item.supplyPercentage)
		return isNaN(value) ? 0 : value
	})
})
</script>
