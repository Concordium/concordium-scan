<template>
	<div>
		<Title>CCDScan | Accounts</Title>
		<div class="">
			<div class="flex flex-row justify-center lg:place-content-end">
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full"
					><CumulativeAccountsCreatedChart
						:account-metrics-data="metricsData"
						:is-loading="metricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<AccountsCreatedChart
						:account-metrics-data="metricsData"
						:is-loading="metricsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="10%">Address</TableTh>
					<TableTh width="20%" class="text-right">Amount (Ͼ)</TableTh>
					<TableTh width="20%">Transaction count</TableTh>
					<TableTh width="20%">Account age</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="account in pagedData" :key="account.address.asString">
					<TableTd>
						<AccountLink :address="account.address.asString" />
					</TableTd>
					<TableTd class="text-right">
						<span class="numerical">
							{{ convertMicroCcdToCcd(account.amount) }}
						</span>
					</TableTd>
					<TableTd>
						<span class="numerical">
							{{ account.transactionCount }}
						</span>
					</TableTd>

					<TableTd>
						<Tooltip :text="formatTimestamp(account.createdAt)">
							{{ convertTimestampToRelative(account.createdAt, NOW) }}
						</Tooltip>
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
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import { MetricsPeriod } from '~/types/generated'
import { useAccountsListQuery } from '~/queries/useAccountListQuery'
import type { Account } from '~/types/generated'
import {
	formatTimestamp,
	convertTimestampToRelative,
	convertMicroCcdToCcd,
} from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import AccountsCreatedChart from '~/components/molecules/ChartCards/AccountsCreatedChart.vue'
import CumulativeAccountsCreatedChart from '~/components/molecules/ChartCards/CumulativeAccountsCreatedChart.vue'

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
const { data: metricsData, fetching: metricsFetching } =
	useAccountsMetricsQuery(selectedMetricsPeriod)
</script>
