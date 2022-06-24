<template>
	<div>
		<Title>CCDScan | Accounts</Title>
		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full">
					<MetricCard class="pt-4">
						<CumulativeAccountsCreatedChart
							:account-metrics-data="metricsData"
							:is-loading="metricsFetching"
						/>
					</MetricCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<AccountsCreatedChart
						:account-metrics-data="metricsData"
						:is-loading="metricsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>

		<header class="flex justify-end w-full mb-4 mt-8 lg:mt-0">
			<AccountSortSelect v-model="sort" />
		</header>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Address</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Account age</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM" align="right">{{
						breakpoint >= Breakpoint.LG ? 'Transactions' : 'Txs'
					}}</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD" align="right">
						Delegated stake <span class="text-theme-faded">(Ͼ)</span>
					</TableTh>
					<TableTh align="right">
						Balance <span class="text-theme-faded">(Ͼ)</span>
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="account in data?.accounts.nodes"
					:key="account.address.asString"
				>
					<TableTd>
						<AccountLink :address="account.address.asString" />
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<Tooltip :text="formatTimestamp(account.createdAt)">
							{{ convertTimestampToRelative(account.createdAt, NOW) }}
						</Tooltip>
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.SM" align="right">
						<span class="numerical">
							{{ account.transactionCount }}
						</span>
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.MD" class="text-right">
						<Amount :amount="account.delegation?.stakedAmount || 0" />
					</TableTd>

					<TableTd class="text-right">
						<Amount :amount="account.amount" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<Pagination
			v-if="data?.accounts.pageInfo"
			:page-info="data?.accounts.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import { MetricsPeriod, AccountSort } from '~/types/generated'
import { useAccountsListQuery } from '~/queries/useAccountListQuery'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { usePagination } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import Amount from '~/components/atoms/Amount.vue'
import MetricCard from '~/components/atoms/MetricCard.vue'
import AccountSortSelect from '~/components/molecules/AccountSortSelect.vue'
import AccountsCreatedChart from '~/components/molecules/ChartCards/AccountsCreatedChart.vue'
import CumulativeAccountsCreatedChart from '~/components/molecules/ChartCards/CumulativeAccountsCreatedChart.vue'
import Pagination from '~/components/Pagination.vue'

const sort = ref<AccountSort>(AccountSort.AmountDesc)
const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage, resetPagination } =
	usePagination()
const { data } = useAccountsListQuery({
	first,
	last,
	after,
	before,
	sort,
})

watch(
	() => sort.value,
	() => resetPagination()
)

const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)
const { data: metricsData, fetching: metricsFetching } =
	useAccountsMetricsQuery(selectedMetricsPeriod)
</script>
