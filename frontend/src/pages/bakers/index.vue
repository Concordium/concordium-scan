<template>
	<div>
		<Title>CCDScan | Bakers</Title>
		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel
				:non-carousel-classes="
					networkType === 'testnet' ? 'grid-cols-4' : 'grid-cols-3'
				"
			>
				<CarouselSlide v-if="networkType === 'testnet'" class="w-full">
					<Payday />
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<TotalAmountStakedChart
						:block-metrics-data="blockMetricsData"
						:is-loading="blockMetricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<TotalBakersChart
						:baker-metrics-data="bakerMetricsData"
						:is-loading="bakerMetricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<TotalRewardsChart
						:reward-metrics-data="rewardMetricsData"
						:is-loading="rewardMetricsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="20%">Baker ID</TableTh>
					<TableTh width="20%">Account</TableTh>
					<TableTh v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						Delegation pool status
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.LG"
						align="right"
					>
						Delegators
					</TableTh>
					<TableTh width="40%" align="right">Staked amount (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="baker in data?.bakers.nodes" :key="baker.bakerId">
					<TableTd>
						<BakerLink :id="baker.bakerId" />
						<Badge
							v-if="baker.state.__typename === 'RemovedBakerState'"
							type="failure"
							class="badge ml-4"
						>
							Removed
						</Badge>
					</TableTd>

					<TableTd>
						<AccountLink :address="baker.account.address.asString" />
					</TableTd>

					<TableTd v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						<Badge
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								composeBakerStatus(baker)?.[0]
							"
							:type="composeBakerStatus(baker)?.[0] || 'success'"
							class="badge"
							variant="secondary"
						>
							{{ composeBakerStatus(baker)?.[1] }}
						</Badge>
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.LG"
						align="right"
					>
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							{{ baker.state.pool?.delegatorCount }}
						</span>
					</TableTd>

					<TableTd class="text-right">
						<Amount
							v-if="baker.state.__typename === 'ActiveBakerState'"
							:amount="baker.state.stakedAmount"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<Pagination
			v-if="data?.bakers.pageInfo"
			:page-info="data?.bakers.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { composeBakerStatus } from '~/utils/composeBakerStatus'
import { usePagination } from '~/composables/usePagination'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import Badge from '~/components/Badge.vue'
import Pagination from '~/components/Pagination.vue'
import Amount from '~/components/atoms/Amount.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import TotalBakersChart from '~/components/molecules/ChartCards/TotalBakersChart.vue'
import Payday from '~/components/molecules/ChartCards/Payday.vue'
import TotalRewardsChart from '~/components/molecules/ChartCards/TotalRewardsChart.vue'
import TotalAmountStakedChart from '~/components/molecules/ChartCards/TotalAmountStakedChart.vue'
import { useBakerListQuery } from '~/queries/useBakerListQuery'
import { useBakerMetricsQuery } from '~/queries/useBakerMetricsQuery'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import { useRewardMetricsQuery } from '~/queries/useRewardMetricsQuery'
import { MetricsPeriod } from '~/types/generated'

const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage } = usePagination()

const { data } = useBakerListQuery({ first, last, after, before })
const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)

const { data: bakerMetricsData, fetching: bakerMetricsFetching } =
	useBakerMetricsQuery(selectedMetricsPeriod)
const { data: rewardMetricsData, fetching: rewardMetricsFetching } =
	useRewardMetricsQuery(selectedMetricsPeriod)
const { data: blockMetricsData, fetching: blockMetricsFetching } =
	useBlockMetricsQuery(selectedMetricsPeriod)

// TODO: This needs to be deleted when paydayStatus hits mainnet
const networkType = location.host.includes('testnet') ? 'testnet' : 'mainnet'

const hasPoolData = computed(() =>
	data.value?.bakers.nodes?.some(
		baker => baker.state.__typename === 'ActiveBakerState' && baker.state.pool
	)
)
</script>

<style scoped>
/*
  These styles could have been TW classes, but are not applied correctly
  A more dynamic approach would be to have a size prop on the component
*/
.badge {
	display: inline-block;
	font-size: 0.75rem;
	padding: 0.4rem 0.5rem 0.25rem;
	margin: 0 1rem 0 0;
	line-height: 1;
}
</style>
