<template>
	<MetricCard class="w-96 lg:w-full" :is-loading="props.isLoading">
		<header class="flex flex-col items-center">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight" />
			</div>

			<div class="text-sm text-theme-faded pt-4 w-72 text-center">
				<slot name="title" />
			</div>
			<div
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
			</div>
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

// import type { StableCoinsQueryResponse } from '~/queries/useStableCoinQuery';
import DoughnutChart from '../Charts/DoughnutChart.vue'

type Props = {
	stableCoinsData: unknown[] | undefined
	isLoading?: boolean
}
const props = defineProps<Props>()
const baseColors = [
	'#2CA783', // Darker & less saturated
	'#33BF97', // Slightly darker
	'#39DBAA', // Original color
	'#4DE3B3', // Slightly lighter
	'#66E8BC', // More vibrant, lighter
	'#7DF0C5', // Even lighter
	'#97F5CE', // Soft pastel shade
	'#B2F9D8', // Very light, soft green
	'#CCFDE2', // Near minty white
	'#E6FFF0', // Faintest, almost white-green
]

const hoverColors = [
	'#238769', // Hover for #2CA783
	'#2AA380', // Hover for #33BF97
	'#2FC295', // Hover for #39DBAA (original)
	'#3DC09A', // Hover for #4DE3B3
	'#55C4A3', // Hover for #66E8BC
	'#6ACCAC', // Hover for #7DF0C5
	'#7DDDB7', // Hover for #97F5CE
	'#97E3C2', // Hover for #B2F9D8
	'#AEEACC', // Hover for #CCFDE2
	'#C4F0DB', // Hover for #E6FFF0
]

const chartLabels = computed(() => {
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	return props.stableCoinsData?.map((item: any) => item.name)
})

const chartData = computed(() => {
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const data = props.stableCoinsData?.map((item: any) =>
		Number(item.value.replace(/,/g, ''))
	)
	// const totalColors = data?.length as number

	return data
})
</script>
