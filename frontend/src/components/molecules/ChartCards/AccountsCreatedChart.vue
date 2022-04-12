<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="accountMetricsData?.accountsMetrics?.buckets?.x_Time"
		:y-values="[
			accountMetricsData?.accountsMetrics?.buckets?.y_AccountsCreated,
		]"
		:bucket-width="accountMetricsData?.accountsMetrics?.buckets?.bucketWidth"
	>
		<template #topRight></template>
		<template #title>Accounts Created</template>
		<template #icon><UserIcon /></template>
		<template #chip>sum</template>
		<template #value>{{
			formatNumber(accountMetricsData?.accountsMetrics?.accountsCreated)
		}}</template>
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
