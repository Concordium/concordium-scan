<template>
	<KPICardContent>
		<template #topRight />
		<template #title>Cumulative Accounts Created</template>
		<template #icon><UserIcon class="w-6 h-6" /></template>
		<template #value>{{
			formatNumber(
				accountMetricsData?.accountsMetrics?.lastCumulativeAccountsCreated
					? accountMetricsData?.accountsMetrics?.lastCumulativeAccountsCreated
					: 0
			)
		}}</template>
		<template #chip>latest</template>
	</KPICardContent>
</template>
<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { formatNumber } from '~/utils/format'
import KPICardContent from '~/components/molecules/KPICardContent.vue'
import type { AccountsMetricsQueryResponse } from '~/queries/useAccountsMetricsQuery'

type Props = {
	accountMetricsData?: AccountsMetricsQueryResponse
}
defineProps<Props>()
</script>
