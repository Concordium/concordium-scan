<template>
	<MetricCard class="w-100 lg:w-full">
		<header class="flex flex-col items-center">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight" />
			</div>

			<div class="text-sm text-theme-faded pt-4 w-72 text-center">
				<slot name="title" />
			</div>
			<!-- <div
				v-if="!props.isLoading"
				class="text-xl text-theme-interactive flex flex-row gap-2"
			>
				<div class="w-6 h-6 mr-2 text-theme-interactive">
					<slot name="icon" />
				</div>
				<div class="numerical">
					<slot name="value" />
				</div>
				<div>
					<slot name="unit" />
				</div>
				<Chip class="self-center">
					<slot name="chip" />
				</Chip>
			</div> -->
		</header>
		<ClientOnly>
			<DoughnutChart
				:data="chartData"
				:labels="chartLabels"
				:background-colors="baseColors"
				:hover-background-colors="hoverColors"
			/>
		</ClientOnly>
	</MetricCard>
</template>

<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import { defineProps, computed } from 'vue'

import type { StablecoinResponse } from '~/queries/useStableCoinQuery'

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
	stableCoinsData?: StablecoinResponse
	isLoading?: boolean
}>()

// Computed Properties
const chartLabels = computed(
	() => props.stableCoinsData?.map(item => item.symbol) || []
)

const chartData = computed(
	() => props.stableCoinsData?.map(item => item.supplyPercentage) || []
)
</script>
