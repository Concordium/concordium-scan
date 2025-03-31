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
	'#FFB3BA', // Soft pastel pink
	'#FFDFBA', // Warm pastel peach
	'#FFFFBA', // Light pastel yellow
	'#BAFFC9', // Mint pastel green
	'#BAE1FF', // Light pastel blue
	'#E0BBE4', // Soft lavender
	'#D4A5A5', // Dusty rose
	'#B5EAD7', // Light mint green
	'#C7CEEA', // Pale periwinkle
	'#F7C8E0', // Light blush pink
]

const hoverColors = [
	'#E89A9A', // Deeper pastel pink
	'#E8C49A', // Soft warm peach
	'#E8E49A', // Rich pastel yellow
	'#9FD9B8', // Deeper mint green
	'#9FC9E8', // Soft sky blue
	'#C49CCF', // Deeper lavender
	'#B08787', // Muted rose
	'#92C5A5', // Subtle mint green
	'#A6A8D1', // Muted periwinkle
	'#DAA6BD', // Soft mauve pink
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
