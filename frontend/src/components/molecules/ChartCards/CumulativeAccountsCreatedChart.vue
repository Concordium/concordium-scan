<template>
	<KPICard class="w-96 lg:w-full">
		<template #topRight></template>
		<template #title>Cumulative Accounts Created</template>
		<template #icon><UserIcon /></template>
		<template #value>{{
			formatNumber(
				accountMetricsData?.accountsMetrics.lastCumulativeAccountsCreated
			)
		}}</template>
		<template #chip>latest</template>
	</KPICard>
</template>
<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { formatNumber } from '~/utils/format'
import KPICard from '~/components/molecules/KPICard.vue'
import type { AccountsMetricsQueryResponse } from '~/queries/useAccountsMetricsQuery'

type Props = {
	accountMetricsData?: AccountsMetricsQueryResponse
}
defineProps<Props>()
</script>
