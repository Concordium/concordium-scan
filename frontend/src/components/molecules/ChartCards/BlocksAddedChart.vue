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
		<template #topRight></template>
		<template #icon><BlockIcon></BlockIcon></template>
		<template #title>Blocks added</template>
		<template #value>{{
			formatNumber(blockMetricsData?.blockMetrics?.blocksAdded)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import BlockIcon from '~/components/icons/BlockIcon.vue'
import { formatNumber } from '~/utils/format'
import type { BlockMetricsQueryResponse } from '~/queries/useChartBlockMetrics'

type Props = {
	blockMetricsData: Ref<BlockMetricsQueryResponse | undefined>
	isLoading?: boolean
}
defineProps<Props>()
</script>
