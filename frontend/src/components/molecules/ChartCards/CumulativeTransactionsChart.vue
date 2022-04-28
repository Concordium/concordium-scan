<template>
	<KPICard>
		<template #title>Cumulative transactions</template>
		<template #icon><TransactionIcon /></template>
		<template #value>{{
			formatNumber(
				transactionMetricsData?.transactionMetrics
					.lastCumulativeTransactionCount
			)
		}}</template>
		<template #chip>latest</template>
	</KPICard>
</template>
<script lang="ts" setup>
import { formatNumber } from '~/utils/format'
import type { TransactionMetricsQueryResponse } from '~/queries/useTransactionMetrics'
import KPICard from '~/components/molecules/KPICard.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'

type Props = {
	transactionMetricsData?: TransactionMetricsQueryResponse
}
defineProps<Props>()
</script>
