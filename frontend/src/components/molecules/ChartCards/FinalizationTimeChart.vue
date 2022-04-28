<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
		:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
		:begin-at-zero="true"
		:y-values="[blockMetricsData?.blockMetrics?.buckets.y_FinalizationTimeAvg]"
		:is-loading="isLoading"
	>
		<template #topRight></template>
		<template #title>Finalization time</template>
		<template #icon><StopwatchIcon /></template>
		<template #value>{{
			formatNumber(blockMetricsData?.blockMetrics?.avgFinalizationTime || 0)
		}}</template>
		<template #unit>s</template>
		<template #chip>average</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import { formatNumber } from '~/utils/format'
import type { BlockMetricsQueryResponse } from '~/queries/useChartBlockMetrics'

type Props = {
	blockMetricsData: Ref<BlockMetricsQueryResponse | undefined>
	isLoading?: boolean
}
defineProps<Props>()
</script>
