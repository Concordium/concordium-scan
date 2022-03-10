<template>
	<div>
		<Title>CCDScan | Accounts</Title>
		<div class="">
			<div class="flex flex-row justify-center lg:place-content-end">
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="metricsData?.accountsMetrics?.buckets?.x_Time"
						:bucket-width="metricsData?.accountsMetrics?.buckets?.bucketWidth"
						:y-values="
							metricsData?.accountsMetrics?.buckets
								?.y_LastCumulativeAccountsCreated
						"
					>
						<template #topRight></template>
						<template #title>Cumulative Accounts Created</template>
						<template #icon><UserIcon /></template>
						<template #value>{{
							formatNumber(
								metricsData?.accountsMetrics?.lastCumulativeAccountsCreated
							)
						}}</template>
						<template #chip>latest</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="metricsData?.accountsMetrics?.buckets?.x_Time"
						:y-values="metricsData?.accountsMetrics?.buckets?.y_AccountsCreated"
						:bucket-width="metricsData?.accountsMetrics?.buckets?.bucketWidth"
					>
						<template #topRight></template>
						<template #title>Accounts Created</template>
						<template #icon><UserIcon /></template>
						<template #chip>sum</template>
						<template #value>{{
							formatNumber(metricsData?.accountsMetrics?.accountsCreated)
						}}</template>
					</KeyValueChartCard>
				</CarouselSlide>
			</FtbCarousel>
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
							{{ convertTimestampToRelative(account.createdAt, NOW) }}
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
	</div>
</template>
<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import { MetricsPeriod } from '~/types/generated'
import { useAccountsListQuery } from '~/queries/useAccountListQuery'
import type { Account } from '~/types/generated'
import { convertTimestampToRelative, formatNumber } from '~/utils/format'
import TransactionLink from '~/components/molecules/TransactionLink.vue'
import { useDateNow } from '~/composables/useDateNow'

const { NOW } = useDateNow()
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
