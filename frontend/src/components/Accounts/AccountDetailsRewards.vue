<template>
	<div>
		<div class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0">
			<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
		</div>
		<RewardMetricsForAccountChart
			:reward-metrics-data="rewardMetricsData"
			:is-loading="rewardMetricsFetching"
			class="mb-20"
		/>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Time</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Type</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XL">Reference</TableTh>
					<TableTh align="right">Amount</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="accountReward in accountRewards"
					:key="accountReward.id"
				>
					<TableTd>
						<Tooltip
							:text="convertTimestampToRelative(accountReward.timestamp, NOW)"
						>
							{{ formatTimestamp(accountReward.timestamp) }}
						</Tooltip>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<div class="whitespace-normal">
							<span class="pl-2">
								<RewardIcon
									class="h-4 text-theme-white inline align-text-top"
								/>
								<span class="pl-2">
									{{ translateBakerRewardType(accountReward.rewardType) }}
								</span>
							</span>
						</div>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XL">
						<BlockLink :hash="accountReward.block.blockHash" />
					</TableTd>
					<TableTd align="right" class="numerical">
						<Amount :amount="accountReward.amount" />
					</TableTd>
				</TableRow>
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
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import RewardIcon from '~/components/icons/RewardIcon.vue'
import { translateBakerRewardType } from '~/utils/translateBakerRewardType'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import {
	type PageInfo,
	type AccountReward,
	Account,
	MetricsPeriod,
} from '~/types/generated'
import { useAccountRewardMetricsQuery } from '~/queries/useAccountRewardMetricsQuery'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import RewardMetricsForAccountChart from '~/components/molecules/ChartCards/RewardMetricsForAccountChart.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	accountRewards: AccountReward[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	accountId: Account['id']
}
const props = defineProps<Props>()

const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)
const { data: rewardMetricsData, fetching: rewardMetricsFetching } =
	useAccountRewardMetricsQuery(props.accountId, selectedMetricsPeriod)
</script>
