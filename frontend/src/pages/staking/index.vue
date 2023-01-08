<template>
	<div>
		<Title>CCDScan | Staking</Title>
		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-4">
				<CarouselSlide class="w-full">
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

		<header
			class="flex flex-wrap justify-between gap-8 w-full mb-4 mt-8 lg:mt-0"
		>
			<div class="flex flex-wrap flex-grow items-center gap-8">
				<TabBar>
					<TabBarItem
						tab-id="bakerPools"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Baker pools
					</TabBarItem>
					<TabBarItem
						tab-id="topDelegators"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Top delegators
					</TabBarItem>
				</TabBar>
				<PassiveDelegationLink />
			</div>

			<div class="flex flex-wrap flex-grow justify-end items-center gap-8">
				<Toggle
					v-if="selectedTab === 'bakerPools'"
					:on-toggle="handleTogglePoolFilter"
					:checked="openStatusFilter === BakerPoolOpenStatus.OpenForAll"
				>
					Show only open pools
				</Toggle>
				<Toggle
					v-if="selectedTab === 'bakerPools'"
					:on-toggle="handleToggleIncludeRemoveFilter"
					:checked="includeRemoved === true"
				>
					Include Removed
				</Toggle>
				<StakingSortSelect
					v-if="selectedTab === 'bakerPools'"
					v-model="tableSort"
					class="justify-self-start"
				/>
			</div>
		</header>

		<BakerPools
			v-if="selectedTab === 'bakerPools'"
			:open-status-filter="openStatusFilter"
			:include-removed="includeRemoved"
			:sort="tableSort"
		/>

		<TopDelegators v-else />
	</div>
</template>
<script lang="ts" setup>
import Toggle from '~/components/atoms/Toggle.vue'
import TabBar from '~/components/atoms/TabBar.vue'
import TabBarItem from '~/components/atoms/TabBarItem.vue'
import PassiveDelegationLink from '~/components/molecules/PassiveDelegationLink.vue'
import StakingSortSelect from '~/components/molecules/StakingSortSelect.vue'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import TotalBakersChart from '~/components/molecules/ChartCards/TotalBakersChart.vue'
import Payday from '~/components/molecules/ChartCards/Payday.vue'
import TotalRewardsChart from '~/components/molecules/ChartCards/TotalRewardsChart.vue'
import TotalAmountStakedChart from '~/components/molecules/ChartCards/TotalAmountStakedChart.vue'
import BakerPools from '~/components/Staking/BakerPools.vue'
import TopDelegators from '~/components/Staking/TopDelegators.vue'
import { useBakerMetricsQuery } from '~/queries/useBakerMetricsQuery'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import { useRewardMetricsQuery } from '~/queries/useRewardMetricsQuery'
import {
	BakerSort,
	BakerPoolOpenStatus,
	MetricsPeriod,
} from '~/types/generated'

const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)
const tableSort = ref<BakerSort>(BakerSort.TotalStakedAmountDesc)
const openStatusFilter = ref<BakerPoolOpenStatus | undefined>(undefined)
const includeRemoved = ref<boolean | undefined>(undefined)
const selectedTab = ref('bakerPools')

const { data: bakerMetricsData, fetching: bakerMetricsFetching } =
	useBakerMetricsQuery(selectedMetricsPeriod)
const { data: rewardMetricsData, fetching: rewardMetricsFetching } =
	useRewardMetricsQuery(selectedMetricsPeriod)
const { data: blockMetricsData, fetching: blockMetricsFetching } =
	useBlockMetricsQuery(selectedMetricsPeriod)

const handleTogglePoolFilter = (checked: boolean) => {
	openStatusFilter.value = checked ? BakerPoolOpenStatus.OpenForAll : undefined
}

const handleToggleIncludeRemoveFilter = (value: boolean) => {
	includeRemoved.value = value ? true : undefined
}

const handleSelectTab = (tabId: string) => (selectedTab.value = tabId)
</script>
