<template>
	<div>
		<Title>CCDScan | Staking</Title>
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
					<TableTh
						v-if="
							(!hasPoolData && breakpoint >= Breakpoint.SM) ||
							breakpoint >= Breakpoint.XL
						"
						width="20%"
					>
						Account
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.SM"
						align="right"
					>
						APY <span class="text-theme-faded">(7 days)</span>
					</TableTh>
					<TableTh v-if="hasPoolData && breakpoint >= Breakpoint.MD">
						Delegation pool status
					</TableTh>
					<TableTh
						v-if="hasPoolData && breakpoint >= Breakpoint.LG"
						align="right"
					>
						Delegators
					</TableTh>
					<TableTh v-if="hasPoolData" align="right">
						Available for delegation
						<span class="text-theme-faded">(Ͼ)</span></TableTh
					>
					<TableTh
						v-if="!hasPoolData || breakpoint >= Breakpoint.LG"
						align="right"
					>
						{{ hasPoolData ? 'Total stake ' : 'Staked amount ' }}
						<span class="text-theme-faded">(Ͼ)</span>
					</TableTh>
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

					<TableTd
						v-if="
							(!hasPoolData && breakpoint >= Breakpoint.SM) ||
							breakpoint >= Breakpoint.XL
						"
					>
						<AccountLink :address="baker.account.address.asString" />
					</TableTd>

					<TableTd
						v-if="hasPoolData && breakpoint >= Breakpoint.SM"
						align="right"
					>
						<Tooltip
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								Number.isFinite(baker.state.pool?.apy.totalApy)
							"
						>
							<template #content>
								<div
									v-if="Number.isFinite(baker.state.pool!.apy.bakerApy)"
									class="grid grid-cols-2"
								>
									<span>Baker:</span>
									<span class="numerical text-theme-faded">
										{{ formatPercentage(baker.state.pool!.apy.bakerApy!) }}%
									</span>
								</div>
								<div
									v-if="Number.isFinite(baker.state.pool!.apy.delegatorsApy)"
									class="grid grid-cols-2"
								>
									<span>Delegators:</span>
									<span class="numerical text-theme-faded">
										{{
											formatPercentage(baker.state.pool!.apy.delegatorsApy!)



										}}%
									</span>
								</div>
							</template>
							<span class="numerical">
								{{ formatPercentage(baker.state.pool!.apy.totalApy!) }}%
							</span>
						</Tooltip>
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

					<TableTd v-if="hasPoolData" align="right">
						<span
							v-if="baker.state.__typename === 'ActiveBakerState'"
							class="numerical"
						>
							<Tooltip
								:text="
									formatDelegationAvailableTooltip(
										baker.state.pool?.delegatedStake,
										baker.state.pool?.delegatedStakeCap
									)
								"
								class="font-sans"
							>
								<Amount
									v-if="
										baker.state.pool && baker.state.pool?.delegatedStake >= 0
									"
									:amount="
										calculateAmountAvailable(
											baker.state.pool.delegatedStake,
											baker.state.pool.delegatedStakeCap
										)
									"
								/>

								<FillBar
									v-if="baker.state.pool"
									:class="[
										calculateDelegatedStakePercent(
											baker.state.pool?.delegatedStake,
											baker.state.pool?.delegatedStakeCap
										) < 10
											? 'bar-warn'
											: 'bar-green-wrapper',
										,
									]"
								>
									<FillBarItem
										:width="
											calculateDelegatedStakePercent(
												baker.state.pool?.delegatedStake,
												baker.state.pool?.delegatedStakeCap
											)
										"
									/>
								</FillBar>
							</Tooltip>
						</span>
					</TableTd>

					<TableTd
						v-if="!hasPoolData || breakpoint >= Breakpoint.LG"
						class="text-right"
					>
						<Tooltip
							v-if="
								baker.state.__typename === 'ActiveBakerState' &&
								baker.state.pool
							"
							text-class="text-left"
						>
							<template #content>
								<div>
									<span class="legend"></span>
									Baker
									<span class="text-theme-faded"
										>({{
											formatPercentage(
												calculatePercentage(
													baker.state.stakedAmount,
													baker.state.pool.totalStake
												) / 100
											)
										}}%)</span
									>
								</div>
								<div v-if="baker.state.pool.delegatedStake">
									<span class="legend legend-green"></span>
									Delegators
									<span class="text-theme-faded"
										>({{
											formatPercentage(
												calculatePercentage(
													baker.state.pool.delegatedStake,
													baker.state.pool.totalStake
												) / 100
											)
										}}%)
									</span>
								</div>
							</template>
							<Amount
								v-if="
									baker.state.__typename === 'ActiveBakerState' &&
									baker.state.pool
								"
								:amount="
									baker.state.pool
										? baker.state.pool.totalStake
										: baker.state.stakedAmount
								"
							/>

							<FillBar
								v-if="
									baker.state.__typename === 'ActiveBakerState' &&
									baker.state.pool
								"
							>
								<FillBarItem
									:width="
										calculatePercentage(
											baker.state.stakedAmount,
											baker.state.pool.totalStake
										)
									"
								/>
								<FillBarItem
									:width="
										calculatePercentage(
											baker.state.pool.delegatedStake,
											baker.state.pool.totalStake
										)
									"
									class="bar-green"
								/>
							</FillBar>
						</Tooltip>
						<Amount
							v-else-if="baker.state.__typename === 'ActiveBakerState'"
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
import FillBar from '~/components/atoms/FillBar.vue'
import FillBarItem from '~~/src/components/atoms/FillBarItem.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
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
import TableTh from '~~/src/components/Table/TableTh.vue'

import { formatPercentage, calculatePercentage } from '~/utils/format'
import {
	calculateAmountAvailable,
	formatDelegationAvailableTooltip,
} from '~/utils/stakingAndDelegation'

const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage } = usePagination()

const { data } = useBakerListQuery({ first, last, after, before })
const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)

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

const calculateDelegatedStakePercent = (
	delegatedStake: number,
	delegatedStakeCap: number
) => calculatePercentage(delegatedStakeCap - delegatedStake, delegatedStakeCap)
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

.bar {
	height: 4px;
	float: left;
}

.bar-warn {
	background-color: hsl(var(--color-error-dark));
}

.bar-warn .bar {
	background-color: hsl(var(--color-error));
}

.bar-green-wrapper .bar,
.bar-green {
	background-color: hsl(var(--color-interactive));
}

.bar-green-wrapper {
	background-color: hsl(var(--color-interactive-dark));
}

.legend {
	display: inline-block;
	width: 10px;
	height: 10px;
	background-color: hsl(var(--color-info));
	margin-right: 0.5em;
}

.legend-green {
	background-color: hsl(var(--color-interactive));
}
</style>
