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
						<template #chip>sum</template>
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
						<template #chip>sum</template>
						<template #value>{{
							metricsData?.accountsMetrics?.accountsCreated
						}}</template>
					</KeyValueChartCard>
				</div>
			</div>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="10%">Address</TableTh>
						<TableTh width="20%">Created At</TableTh>
						<TableTh width="20%">Latest transaction</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="account in pagedData" :key="account.address">
						<TableTd>
							<AccountLink :address="account.address" />
						</TableTd>
						<TableTd>
							<Tooltip :text="account.createdAt">
								{{ convertTimestampToRelative(account.createdAt) }}
							</Tooltip>
						</TableTd>
						<TableTd>
							<div v-if="account?.transactions?.nodes?.length > 0">
								<TransactionLink
									:id="account?.transactions?.nodes[0].transaction.id"
									:hash="
										account?.transactions?.nodes[0].transaction.transactionHash
									"
								/>
							</div>
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>

			<LoadMore
				v-if="data?.accounts.pageInfo"
				:page-info="data?.accounts.pageInfo"
				:on-load-more="loadMore"
			/>
		</main>
	</div>
</template>
<script lang="ts" setup>
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import { MetricsPeriod } from '~/types/generated'
import { useAccountsListQuery } from '~/queries/useAccountListQuery'
import type { Account } from '~/types/generated'
import { convertTimestampToRelative } from '~/utils/format'
import TransactionLink from '~/components/molecules/TransactionLink.vue'
const { pagedData, first, last, after, before, addPagedData, loadMore } =
	usePagedData<Account>()
const { data } = useAccountsListQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.accounts.nodes || [], value?.accounts.pageInfo)
	}
)

const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const { data: metricsData } = useAccountsMetricsQuery(selectedMetricsPeriod)
</script>
