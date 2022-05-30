<template>
	<div>
		<Title>CCDScan | Bakers</Title>
		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-3">
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
					<TableTh v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						Status
					</TableTh>
					<TableTh width="20%">Account</TableTh>
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
							v-if="
								!hasPoolData && baker.state.__typename === 'RemovedBakerState'
							"
							type="failure"
							class="badge ml-4"
						>
							Removed
						</Badge>
					</TableTd>

					<TableTd v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						<Badge
							v-if="composeBakerStatus(baker)?.[0]"
							:type="composeBakerStatus(baker)?.[0] || 'success'"
							class="badge"
						>
							{{ composeBakerStatus(baker)?.[1] }}
						</Badge>
					</TableTd>

					<TableTd>
						<AccountLink :address="baker.account.address.asString" />
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.LG"
						align="right"
					>
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numeric"
						>
							{{ baker.state.pool?.delegatorCount }}
						</span>
					</TableTd>

					<TableTd class="text-right">
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							{{ convertMicroCcdToCcd(baker.state.stakedAmount) }}
						</span>
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
import { useBakerListQuery } from '~/queries/useBakerListQuery'
import { convertMicroCcdToCcd } from '~/utils/format'
import { composeBakerStatus } from '~/utils/composeBakerStatus'
import { usePagination } from '~/composables/usePagination'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import Badge from '~/components/Badge.vue'
import Pagination from '~/components/Pagination.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import { MetricsPeriod } from '~/types/generated'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import TotalBakersChart from '~/components/molecules/ChartCards/TotalBakersChart.vue'
import { useBakerMetricsQuery } from '~/queries/useBakerMetricsQuery'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import { useRewardMetricsQuery } from '~/queries/useRewardMetricsQuery'
import TotalRewardsChart from '~/components/molecules/ChartCards/TotalRewardsChart.vue'
import TotalAmountStakedChart from '~/components/molecules/ChartCards/TotalAmountStakedChart.vue'

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
	margin: 0;
	line-height: 1;
}
</style>
