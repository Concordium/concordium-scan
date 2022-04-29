<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
		:y-values="[
			blockMetricsData?.blockMetrics?.buckets?.y_LastTotalMicroCcdStaked,
		]"
		:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
		chart-type="line"
		:begin-at-zero="true"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight></template>
		<template #icon>Ͼ</template>
		<template #title>Staked</template>
		<template #value>{{
			convertMicroCcdToCcd(
				blockMetricsData?.blockMetrics?.lastTotalMicroCcdStaked
			)
		}}</template>
		<template #chip>latest</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import type { TooltipItem } from 'chart.js'
import type { BlockMetricsQueryResponse } from '~/queries/useChartBlockMetrics'
import { convertMicroCcdToCcd } from '~/utils/format'
type Props = {
	blockMetricsData: Ref<BlockMetricsQueryResponse | undefined>
	isLoading?: boolean
}
const formatLabel = (c: TooltipItem<'line'>) => {
	return convertMicroCcdToCcd(c.parsed.y)
}
defineProps<Props>()
</script>
