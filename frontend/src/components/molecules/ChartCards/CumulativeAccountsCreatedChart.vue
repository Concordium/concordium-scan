<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:x-values="accountMetricsData?.accountsMetrics?.buckets?.x_Time"
		:bucket-width="accountMetricsData?.accountsMetrics?.buckets?.bucketWidth"
		:y-values="[
			accountMetricsData?.accountsMetrics?.buckets
				?.y_LastCumulativeAccountsCreated,
		]"
	>
		<template #topRight></template>
		<template #title>Cumulative Accounts Created</template>
		<template #icon><UserIcon /></template>
		<template #value>{{
			formatNumber(
				accountMetricsData?.accountsMetrics?.lastCumulativeAccountsCreated
			)
		}}</template>
		<template #chip>latest</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { formatNumber } from '~/utils/format'

import type { AccountsMetricsQueryResponse } from '~/queries/useAccountsMetricsQuery'

type Props = {
	accountMetricsData: Ref<AccountsMetricsQueryResponse | undefined>
}
defineProps<Props>()
</script>
