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
import type { PltTransferMetricsQueryResponse } from '~/queries/usePltTransferMetricsQuery'

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
	transferSummary?: PltTransferMetricsQueryResponse
	decimals: number
}>()

// Computed Properties
const chartLabels = computed(() => {
	const interval =
		props.transferSummary?.pltTransferMetricsByTokenId.buckets.bucketWidth
	switch (interval) {
		case 'PT1H':
			return (
				props.transferSummary?.pltTransferMetricsByTokenId.buckets[
					'x_Time'
				].map((item: string) =>
					new Date(item).toLocaleDateString('en-UK', {
						hour: '2-digit',
						minute: '2-digit',
						month: 'short',
						day: 'numeric',
						year: 'numeric',
					})
				) || []
			)

		case 'PT6H':
			return (
				props.transferSummary?.pltTransferMetricsByTokenId.buckets[
					'x_Time'
				].map((item: string) =>
					new Date(item).toLocaleDateString('en-UK', {
						hour: '2-digit',
						minute: '2-digit',
						month: 'short',
						day: 'numeric',
						year: 'numeric',
					})
				) || []
			)
		case 'P1D':
			return (
				props.transferSummary?.pltTransferMetricsByTokenId.buckets[
					'x_Time'
				].map((item: string) =>
					new Date(item).toLocaleDateString('en-UK', {
						month: 'short',
						day: 'numeric',
						year: 'numeric',
					})
				) || []
			)
		case 'P3D':
			return (
				props.transferSummary?.pltTransferMetricsByTokenId.buckets[
					'x_Time'
				].map(
					(item: string) =>
						new Date(item).toLocaleDateString('en-UK', {
							day: 'numeric',
						}) +
						' - ' +
						new Date(
							new Date(item).getTime() + 24 * 2 * 60 * 60 * 1000
						).toLocaleDateString('en-UK', {
							month: 'short',
							day: 'numeric',
							year: 'numeric',
						})
				) || []
			)
		case 'P15D':
			return (
				props.transferSummary?.pltTransferMetricsByTokenId.buckets[
					'x_Time'
				].map(
					(item: string) =>
						new Date(item).toLocaleDateString('en-UK', {
							day: 'numeric',
						}) +
						' - ' +
						new Date(
							new Date(item).getTime() + 24 * 14 * 60 * 60 * 1000
						).toLocaleDateString('en-UK', {
							month: 'short',
							day: 'numeric',
							year: 'numeric',
						})
				) || []
			)
		default:
			return []
	}
})

const barGraphValues = computed<number[]>(
	() =>
		props.transferSummary?.pltTransferMetricsByTokenId.buckets
			.y_TransferCount ?? []
)

const lineGraphValues = computed<number[]>(
	() =>
		props.transferSummary?.pltTransferMetricsByTokenId.buckets.y_TransferVolume.map(
			(item: number) => item / Math.pow(10, props.decimals)
		) ?? []
)
</script>
