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
			<ChartBarST
				class="h-28 w-full"
				:y-values="chartData"
				:x-values="chartLabels"
				:begin-at-zero="true"
			/>
		</ClientOnly>
	</MetricCard>
</template>

<script lang="ts" setup>
import MetricCard from '~/components/atoms/MetricCard.vue'
import { defineProps, computed } from 'vue'

// import type { StableCoinsQueryResponse } from '~/queries/useStableCoinQuery';
import ChartBarST from '../Charts/ChartBarST.vue'

type Props = {
	stableCoinsData: unknown[] | undefined
	isLoading?: boolean
}
const props = defineProps<Props>()

const chartLabels = computed(() => {
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	return props.stableCoinsData?.map((item: any) => item.name)
})

const chartData = computed(() => {
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	const data = props.stableCoinsData?.map((item: any) =>
		Number(item.value.replace(/,/g, ''))
	)

	return data
})
</script>
