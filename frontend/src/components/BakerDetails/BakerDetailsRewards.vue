<template>
	<div class="w-full">
		<div v-if="componentState === 'success' || componentState === 'loading'">
			<div
				v-if="componentState === 'success'"
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
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
						<TableTh align="right">Amount (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody v-if="componentState === 'success'">
					<TableRow
						v-for="reward in data?.accountByAddress.rewards?.nodes || []"
						:key="reward.id"
					>
						<TableTd>
							<Tooltip
								:text="convertTimestampToRelative(reward.timestamp, NOW)"
							>
								{{ formatTimestamp(reward.timestamp) }}
							</Tooltip>
						</TableTd>
						<TableTd v-if="breakpoint >= Breakpoint.LG">
							<div class="whitespace-normal">
								<span class="pl-2">
									<RewardIcon
										class="h-4 text-theme-white inline align-text-top"
									/>
									<span class="pl-2">
										{{ translateBakerRewardType(reward.rewardType) }}
									</span>
								</span>
							</div>
						</TableTd>
						<TableTd v-if="breakpoint >= Breakpoint.XL">
							<BlockLink :hash="reward.block.blockHash" />
						</TableTd>
						<TableTd class="numerical" align="right">
							<Amount :amount="reward.amount" />
						</TableTd>
					</TableRow>
				</TableBody>

				<TableBody v-else-if="componentState === 'loading'">
					<TableRow>
						<TableTd colspan="3">
							<div v-if="componentState === 'loading'" class="relative h-48">
								<Loader />
							</div>
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
		</div>

		<NotFound v-else-if="componentState === 'empty'">
			No rewards
			<template #secondary> There are no rewards for this validator </template>
		</NotFound>
		<Error v-else-if="componentState === 'error'" :error="error" />

		<Pagination
			v-if="
				componentState === 'success' &&
				(pageInfo?.hasNextPage || pageInfo?.hasPreviousPage)
			"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useDateNow } from '~/composables/useDateNow'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateBakerRewardType } from '~/utils/translateBakerRewardType'
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import Pagination from '~/components/Pagination.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'
import type { PageInfo } from '~/types/generated'
import { useBakerRewardsQuery } from '~/queries/useBakerRewardsQuery'
import BlockLink from '~/components/molecules/BlockLink.vue'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import RewardIcon from '~/components/icons/RewardIcon.vue'

import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import { MetricsPeriod } from '~/types/generated'
import { useAccountRewardMetricsQuery } from '~/queries/useAccountRewardMetricsQuery'
import RewardMetricsForAccountChart from '~/components/molecules/ChartCards/RewardMetricsForAccountChart.vue'

const { first, last, after, before, goToPage } = usePagination({
	pageSize: PAGE_SIZE_SMALL,
})

const { NOW } = useDateNow()

type Props = {
	accountAddress: string
	accountId: string
}

const props = defineProps<Props>()

const { data, error, componentState } = useBakerRewardsQuery(
	props.accountAddress,
	{
		first,
		last,
		after,
		before,
	}
)

const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)

const { data: rewardMetricsData, fetching: rewardMetricsFetching } =
	useAccountRewardMetricsQuery(props.accountId, selectedMetricsPeriod)

const pageInfo = ref<PageInfo | undefined>(
	data?.value?.accountByAddress?.rewards?.pageInfo
)
const { breakpoint } = useBreakpoint()
watch(
	() => data.value,
	value => (pageInfo.value = value?.accountByAddress?.rewards?.pageInfo)
)
</script>
