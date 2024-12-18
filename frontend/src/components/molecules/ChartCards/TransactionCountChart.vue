<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="transactionMetricsData?.transactionMetrics?.buckets?.x_Time"
		:bucket-width="
			transactionMetricsData?.transactionMetrics?.buckets?.bucketWidth
		"
		:y-values="[
			transactionMetricsData?.transactionMetrics?.buckets?.y_TransactionCount,
		]"
		:is-loading="isLoading"
	>
		<template #topRight />
		<template #title>Transactions</template>
		<template #icon><TransactionIcon /></template>
		<template #value>{{
			formatNumber(
				transactionMetricsData?.transactionMetrics?.transactionCount || 0
			)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import { formatNumber } from '~/utils/format'
import type { TransactionMetricsQueryResponse } from '~/queries/useTransactionMetrics'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'

type Props = {
	transactionMetricsData: TransactionMetricsQueryResponse | undefined
	isLoading?: boolean
}
defineProps<Props>()
</script>
