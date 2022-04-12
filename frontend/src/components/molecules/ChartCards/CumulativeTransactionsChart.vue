<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="transactionMetricsData?.transactionMetrics?.buckets?.x_Time"
		:bucket-width="
			transactionMetricsData?.transactionMetrics?.buckets?.bucketWidth
		"
		:y-values="[
			transactionMetricsData?.transactionMetrics?.buckets
				?.y_LastCumulativeTransactionCount,
		]"
	>
		<template #topRight></template>
		<template #title>Cumulative Transactions</template>
		<template #icon><TransactionIcon /></template>
		<template #value>{{
			formatNumber(
				transactionMetricsData?.transactionMetrics
					?.lastCumulativeTransactionCount
			)
		}}</template>
		<template #chip>latest</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import { formatNumber } from '~/utils/format'
import type { TransactionMetricsQueryResponse } from '~/queries/useTransactionMetrics'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'

type Props = {
	transactionMetricsData: Ref<TransactionMetricsQueryResponse | undefined>
}
defineProps<Props>()
</script>
