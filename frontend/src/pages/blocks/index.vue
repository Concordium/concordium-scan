<template>
	<div>
		<Title>CCDScan | Blocks</Title>
		<div class="">
			<div class="flex flex-row justify-center lg:place-content-end">
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="metricsData?.blockMetrics?.buckets?.x_Time"
						:y-values="[metricsData?.blockMetrics?.buckets?.y_BlocksAdded]"
						:bucket-width="metricsData?.blockMetrics?.buckets?.bucketWidth"
					>
						<template #topRight></template>
						<template #icon><BlockIcon></BlockIcon></template>
						<template #title>Blocks added</template>
						<template #value>{{
							formatNumber(metricsData?.blockMetrics?.blocksAdded)
						}}</template>
						<template #chip>sum</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="metricsData?.blockMetrics?.buckets?.x_Time"
						:bucket-width="metricsData?.blockMetrics?.buckets?.bucketWidth"
						chart-type="area"
						:y-values="[
							metricsData?.blockMetrics?.buckets?.y_BlockTimeMax,
							metricsData?.blockMetrics?.buckets?.y_BlockTimeAvg,
							metricsData?.blockMetrics?.buckets?.y_BlockTimeMin,
						]"
					>
						<template #topRight></template>
						<template #title>Block time</template>
						<template #icon><StopwatchIcon /></template>
						<template #value>{{
							formatNumber(metricsData?.blockMetrics?.avgBlockTime)
						}}</template>
						<template #unit>s</template>
						<template #chip>average</template>
					</KeyValueChartCard>
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
						Timestamp
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
						<StatusCircle
							:class="[
								'h-4 w-6 mr-2 text-theme-interactive',
								{ 'text-theme-info': !block.finalized },
							]"
						/>
						<span v-if="breakpoint >= Breakpoint.SM">
							{{ block.finalized ? 'Finalised' : 'Pending' }}
						</span>
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
						<Baker :id="block.bakerId" />
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
import BlockIcon from '~/components/icons/BlockIcon.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import {
	formatTimestamp,
	convertTimestampToRelative,
	formatNumber,
} from '~/utils/format'
import { usePagedData } from '~/composables/usePagedData'
import { useDateNow } from '~/composables/useDateNow'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import { MetricsPeriod, type Block, type Subscription } from '~/types/generated'
import StopwatchIcon from '~/components/icons/StopwatchIcon.vue'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import Baker from '~/components/molecules/Baker.vue'

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
const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
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

const { data: metricsData } = useBlockMetricsQuery(selectedMetricsPeriod)
</script>

<style>
.cardShadow {
	filter: drop-shadow(0px 24px 38px rgba(0, 0, 0, 0.14))
		drop-shadow(0px 9px 46px rgba(0, 0, 0, 0.12))
		drop-shadow(0px 11px 15px rgba(0, 0, 0, 0.2));
}
</style>
