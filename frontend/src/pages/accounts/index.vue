<template>
	<div>
		<Title>CCDScan | Accounts</Title>

		<main class="p-4 pb-0">
			<div class="block lg:grid grid-cols-2">
				<div class="w-full">
					<KeyValueChartCard
						:x-values="metricsData?.accountsMetrics?.buckets?.x_Time"
						:bucket-width="metricsData?.accountsMetrics?.buckets?.bucketWidth"
						:y-values="
							metricsData?.accountsMetrics?.buckets
								?.y_LastCumulativeAccountsCreated
						"
					>
						<template #topRight
							><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
						/></template>
						<template #title>Last Cumulative Accounts Created</template>
						<template #icon></template>
						<template #value>{{
							metricsData?.accountsMetrics?.lastCumulativeAccountsCreated
						}}</template>
					</KeyValueChartCard>
				</div>
				<div class="w-full">
					<KeyValueChartCard
						:x-values="metricsData?.accountsMetrics?.buckets?.x_Time"
						:y-values="metricsData?.accountsMetrics?.buckets?.y_AccountsCreated"
						:bucket-width="metricsData?.accountsMetrics?.buckets?.bucketWidth"
					>
						<template #topRight
							><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
						/></template>
						<template #title>Accounts Created</template>
						<template #icon></template>
						<template #value>{{
							metricsData?.accountsMetrics?.accountsCreated
						}}</template>
					</KeyValueChartCard>
				</div>
			</div>
		</main>
	</div>
</template>
<script lang="ts" setup>
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import { MetricsPeriod } from '~/types/generated'

const selectedMetricsPeriod = ref(MetricsPeriod.LastHour)
const { data: metricsData } = useAccountsMetricsQuery(selectedMetricsPeriod)
</script>
