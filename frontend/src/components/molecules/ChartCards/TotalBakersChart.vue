<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="line"
		:x-values="bakerMetricsData?.bakerMetrics?.buckets?.x_Time"
		:bucket-width="bakerMetricsData?.bakerMetrics?.buckets?.bucketWidth"
		:y-values="[bakerMetricsData?.bakerMetrics?.buckets?.y_LastBakerCount]"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight></template>
		<template #title>Bakers</template>
		<template #icon><TransactionIcon /></template>
		<template #value>{{
			formatNumber(bakerMetricsData?.bakerMetrics?.lastBakerCount || 0)
		}}</template>
		<template #chip>latest</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { TooltipItem } from 'chart.js'
import { formatNumber } from '~/utils/format'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import type { BakerMetricsQueryResponse } from '~/queries/useBakerMetricsQuery'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'

type Props = {
	bakerMetricsData: BakerMetricsQueryResponse | undefined
	isLoading?: boolean
}
const props = defineProps<Props>()

const formatLabel = (c: TooltipItem<'bar'>) => {
	const added =
		props.bakerMetricsData?.bakerMetrics?.buckets?.y_BakersAdded[c.parsed.x]
	const removed =
		props.bakerMetricsData?.bakerMetrics?.buckets?.y_BakersRemoved[c.parsed.x]
	let label = c.parsed.y + ''

	if (added === undefined || removed === undefined) return label

	if (added > 0 && removed > 0) label += ` (${added} added, ${removed} removed)`
	else if (added > 0) label += ` (${added} added)`
	else if (removed > 0) label += ` (${removed} removed)`
	return label
}
</script>
