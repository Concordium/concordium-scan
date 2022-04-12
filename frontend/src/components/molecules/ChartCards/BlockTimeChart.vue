<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
		:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
		:begin-at-zero="true"
		:y-values="[blockMetricsData?.blockMetrics?.buckets?.y_BlockTimeAvg]"
	>
		<template #topRight></template>
		<template #title>Block time</template>
		<template #icon><StopwatchIcon /></template>
		<template #value>{{
			blockMetricsData?.blockMetrics?.avgBlockTime
		}}</template>
		<template #unit>s</template>
		<template #chip>average</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import type { BlockMetricsQueryResponse } from '~/queries/useChartBlockMetrics'

type Props = {
	blockMetricsData: Ref<BlockMetricsQueryResponse | undefined>
}
defineProps<Props>()
</script>
