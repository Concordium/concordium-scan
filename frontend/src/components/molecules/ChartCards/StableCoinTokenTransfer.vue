<template>
	<div>
		<MetricCard class="w-96 lg:w-full">
			<header class="flex flex-col items-center">
				<div class="absolute top-4 right-4 text-xs">
					<slot name="topRight" />
				</div>

				<div class="text-sm text-theme-faded pt-4 w-72 text-center">
					Token Transfer
				</div>
			</header>
			<ClientOnly>
				<ChartBarLine
					:bar-data="barGraphValues"
					:line-data="lineGraphValues"
					:labels="chartLabels"
					:background-colors="baseColors"
					:hover-background-colors="hoverColors"
				/>
			</ClientOnly>
		</MetricCard>
	</div>
</template>
<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import { defineProps, computed } from 'vue'
import ChartBarLine from '~/components/Charts/ChartBarLine.vue'
import type { StableCoinTokenTransferResponse } from '~/queries/useStableCoinTokenTransferQuery'

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
	transferSummary?: StableCoinTokenTransferResponse
	isLoading?: boolean
}>()

// Computed Properties
const chartLabels = computed(
	() =>
		props.transferSummary?.transferSummary?.dailySummary?.map(
			item => item.date
		) || []
)

const barGraphValues = computed(
	() =>
		props.transferSummary?.transferSummary?.dailySummary?.map(
			item => item.transactionCount
		) || []
)

const lineGraphValues = computed(
	() =>
		props.transferSummary?.transferSummary?.dailySummary?.map(
			item => item.totalAmount
		) || []
)
</script>
