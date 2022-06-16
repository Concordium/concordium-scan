<template>
	<div class="w-full">
		<div v-if="componentState === 'success' || componentState === 'loading'">
			<div
				v-if="componentState === 'success'"
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0 gap-4"
			>
				<RewardTakerTypeDropdown v-model="selectedRewardTakerType" />
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<RewardMetricsForPoolChart
				v-if="componentState === 'success'"
				:reward-metrics-data="rewardMetricsForBakerData"
				:is-loading="rewardMetricsForBakerFetching"
				:reward-taker-type="selectedRewardTakerType"
			/>

			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Time</TableTh>
						<TableTh v-if="breakpoint >= Breakpoint.MD">Reference</TableTh>
						<TableTh align="right">Rewards (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody
					v-if="
						componentState === 'success' &&
						data?.bakerByBakerId.state.__typename === 'ActiveBakerState'
					"
				>
					<BakerDetailsPoolRewardsItem
						v-for="reward in data?.bakerByBakerId.state.pool?.poolRewards
							?.nodes || []"
						:key="reward.id"
						:reward="reward"
					/>
				</TableBody>

				<TableBody v-else-if="componentState === 'loading'">
					<TableRow>
						<TableTd colspan="4">
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
			<template #secondary> There are no rewards for this baker </template>
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
import BakerDetailsPoolRewardsItem from './BakerDetailsPoolRewardsItem.vue'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'

import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import RewardMetricsForPoolChart from '~/components/molecules/ChartCards/RewardMetricsForPoolChart.vue'
import RewardTakerTypeDropdown from '~/components/molecules/RewardTakerTypeDropdown.vue'
import Pagination from '~/components/Pagination.vue'
import Table from '~/components/Table/Table.vue'
import TableTd from '~/components/Table/TableTd.vue'
import TableTh from '~/components/Table/TableTh.vue'
import TableRow from '~/components/Table/TableRow.vue'
import TableBody from '~/components/Table/TableBody.vue'
import TableHead from '~/components/Table/TableHead.vue'

import { useBakerPoolRewardsQuery } from '~/queries/useBakerPoolRewardsQuery'
import { useBakerPoolRewardMetrics } from '~/queries/useBakerPoolRewardMetrics'
import { RewardTakerTypes } from '~/types/rewardTakerTypes'
import { MetricsPeriod } from '~/types/generated'
import type { Baker, PageInfo } from '~/types/generated'

const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage } = usePagination({
	pageSize: PAGE_SIZE_SMALL,
})

type Props = {
	bakerId: Baker['bakerId']
	rawId: Baker['id']
}

const props = defineProps<Props>()

const { data, error, componentState } = useBakerPoolRewardsQuery(
	props.bakerId,
	{
		first,
		last,
		after,
		before,
	}
)
const selectedRewardTakerType = ref(RewardTakerTypes.Total)
const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const {
	data: rewardMetricsForBakerData,
	fetching: rewardMetricsForBakerFetching,
} = useBakerPoolRewardMetrics(props.rawId, selectedMetricsPeriod)

const pageInfo = ref<PageInfo | undefined>(
	data.value?.bakerByBakerId.state.__typename === 'ActiveBakerState'
		? data?.value?.bakerByBakerId?.state.pool?.poolRewards?.pageInfo
		: undefined
)

watch(
	() => data.value,
	value => {
		if (value?.bakerByBakerId.state.__typename === 'ActiveBakerState') {
			pageInfo.value = value?.bakerByBakerId?.state.pool?.poolRewards?.pageInfo
		}
	}
)
</script>
