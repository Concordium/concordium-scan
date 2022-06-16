<template>
	<div>
		<div class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0">
			<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
		</div>
		<PoolRewardTotalChart
			:reward-metrics-data="rewardMetricsData"
			:is-loading="rewardMetricsFetching"
			class="mb-20"
		/>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Time</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD">Reference</TableTh>
					<TableTh align="right">Rewards (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<PassiveDelegationRewardsItem
					v-for="reward in poolRewards || []"
					:key="reward.id"
					:reward="reward"
				/>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import PassiveDelegationRewardsItem from './PassiveDelegationRewardsItem.vue'
import { translateBakerRewardType } from '~/utils/translateBakerRewardType'
import { usePassiveDelegationPoolRewardMetrics } from '~/queries/usePassiveDelegationPoolRewardMetrics'
import PoolRewardTotalChart from '~/components/molecules/ChartCards/PoolRewardTotalChart.vue'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import type { PageInfo, PaydayPoolReward } from '~/types/generated'
import { MetricsPeriod } from '~/types/generated'

const { breakpoint } = useBreakpoint()

type Props = {
	poolRewards: PaydayPoolReward[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()

const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const { data: rewardMetricsData, fetching: rewardMetricsFetching } =
	usePassiveDelegationPoolRewardMetrics(selectedMetricsPeriod)
</script>
