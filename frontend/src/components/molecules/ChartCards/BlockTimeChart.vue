<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
		:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
		:begin-at-zero="true"
		:y-values="[blockMetricsData?.blockMetrics?.buckets?.y_BlockTimeAvg]"
		:is-loading="isLoading"
	>
		<template #topRight />
		<template #title>Block time</template>
		<template #icon><StopwatchIcon /></template>
		<template #value>{{
			blockMetricsData?.blockMetrics?.avgBlockTime
				? formatNumber(blockMetricsData?.blockMetrics?.avgBlockTime)
				: '-'
		}}</template>
		<template #unit>s</template>
		<template #chip>average</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { BlockMetricsQueryResponse } from '~/queries/useChartBlockMetrics'
import { formatNumber } from '~/utils/format'

type Props = {
	blockMetricsData?: BlockMetricsQueryResponse
	isLoading?: boolean
}
defineProps<Props>()
</script>
