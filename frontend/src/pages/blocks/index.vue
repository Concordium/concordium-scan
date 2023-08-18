<template>
	<div>
		<Title>CCDScan | Blocks</Title>
		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full">
					<BlocksAddedChart
						:block-metrics-data="metricsData"
						:is-loading="metricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<BlockTimeChart
						:block-metrics-data="metricsData"
						:is-loading="metricsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="10%">Block hash</TableTh>
					<TableTh width="20%">Status</TableTh>
					<TableTh width="20%">Height</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD" width="30%">
						Age
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG" width="10%">
						Baker
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM" width="10%" align="right">
						Transactions
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow>
					<TableTd colspan="6" align="center" class="p-0 td-no-xpadding">
						<ShowMoreButton :new-item-count="newItems" :refetch="refetch" />
					</TableTd>
				</TableRow>
				<TableRow v-for="block in pagedData" :key="block.blockHash">
					<TableTd>
						<BlockLink :id="block.id" :hash="block.blockHash" />
					</TableTd>
					<TableTd>
						<BlockFinalized :finalized="block.finalized" :show-text="true" />
					</TableTd>
					<TableTd class="numerical">
						{{ block.blockHeight }}
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.MD">
						<Tooltip :text="formatTimestamp(block.blockSlotTime)">
							{{ convertTimestampToRelative(block.blockSlotTime, NOW) }}
						</Tooltip>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG" class="numerical">
						<BakerLink
							v-if="block.bakerId || block.bakerId === 0"
							:id="block.bakerId"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.SM"
						align="right"
						class="numerical"
					>
						{{ block.transactionCount }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<LoadMore
			v-if="data?.blocks.pageInfo"
			:page-info="data?.blocks.pageInfo"
			:on-load-more="loadMore"
		/>
	</div>
</template>

<script lang="ts" setup>
import Tooltip from '~/components/atoms/Tooltip.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { usePagedData } from '~/composables/usePagedData'
import { useDateNow } from '~/composables/useDateNow'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import { MetricsPeriod, type Block, type Subscription } from '~/types/generated'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import BlockFinalized from '~/components/molecules/BlockFinalized.vue'
import BlocksAddedChart from '~/components/molecules/ChartCards/BlocksAddedChart.vue'
import BlockTimeChart from '~/components/molecules/ChartCards/BlockTimeChart.vue'
import FinalizationTimeChart from '~/components/molecules/ChartCards/FinalizationTimeChart.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

const {
	pagedData,
	first,
	last,
	after,
	before,
	addPagedData,
	fetchNew,
	loadMore,
} = usePagedData<Block>()
const newItems = ref(0)
const subscriptionHandler = (_prevData: void, _newData: Subscription) => {
	newItems.value++
}
const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)
const { pause: pauseSubscription, resume: resumeSubscription } =
	useBlockSubscription(subscriptionHandler)

const refetch = () => {
	fetchNew(newItems.value)
	newItems.value = 0
}
onMounted(() => {
	resumeSubscription()
})
onUnmounted(() => {
	pauseSubscription()
})
const { data } = useBlockListQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.blocks.nodes || [], value?.blocks.pageInfo)
	}
)

const { data: metricsData, fetching: metricsFetching } = useBlockMetricsQuery(
	selectedMetricsPeriod
)
</script>

<style>
.cardShadow {
	filter: drop-shadow(0px 24px 38px rgba(0, 0, 0, 0.14))
		drop-shadow(0px 9px 46px rgba(0, 0, 0, 0.12))
		drop-shadow(0px 11px 15px rgba(0, 0, 0, 0.2));
}
</style>
