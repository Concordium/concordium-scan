<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="line"
		:x-values="bakerMetricsData?.bakerMetrics?.buckets?.x_Time"
		:bucket-width="bakerMetricsData?.bakerMetrics?.buckets?.bucketWidth"
		:y-values="[
			bakerMetricsData?.bakerMetrics?.buckets?.y_LastBakerCount,

			bakerMetricsData?.bakerMetrics?.buckets?.y_BakersAdded,
			bakerMetricsData?.bakerMetrics?.buckets?.y_BakersRemoved,
		]"
		:is-loading="isLoading"
	>
		<template #topRight></template>
		<template #title>Bakers</template>
		<template #icon><TransactionIcon /></template>
		<template #value>{{
			formatNumber(bakerMetricsData?.bakerMetrics?.lastBakerCount)
		}}</template>
		<template #chip>latest</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import { formatNumber } from '~/utils/format'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import type { BakerMetricsQueryResponse } from '~/queries/useBakerMetricsQuery'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'

type Props = {
	bakerMetricsData: Ref<BakerMetricsQueryResponse | undefined>
	isLoading?: boolean
}
defineProps<Props>()
</script>
