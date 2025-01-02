<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
		:y-values="[blockMetricsData?.blockMetrics?.buckets?.y_BlocksAdded]"
		:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
		chart-type="bar"
		:begin-at-zero="true"
		:is-loading="isLoading"
	>
		<template #topRight />
		<template #icon><BlockIcon /></template>
		<template #title>Blocks added</template>
		<template #value>{{
			formatNumber(blockMetricsData?.blockMetrics?.blocksAdded || 0)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import BlockIcon from '~/components/icons/BlockIcon.vue'
import { formatNumber } from '~/utils/format'
import type { BlockMetricsQueryResponse } from '~/queries/useChartBlockMetrics'

type Props = {
	blockMetricsData: BlockMetricsQueryResponse | undefined
	isLoading?: boolean
}
defineProps<Props>()
</script>
