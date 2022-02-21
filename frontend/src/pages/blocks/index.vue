<template>
	<div>
		<Title>CCDScan | Blocks</Title>
		<main class="p-4 pb-0">
			<div
				v-if="
					metricsData &&
					metricsData.blockMetrics &&
					metricsData.blockMetrics.buckets
				"
				class="block lg:grid grid-cols-4"
			>
				<div class="w-full">
					<KeepAlive>
						<KeyValueChartCard
							:x-values="metricsData.blockMetrics.buckets.x_Time"
							:y-values="metricsData.blockMetrics.buckets.y_BlocksAdded"
						>
							<template #topRight
								><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
							/></template>
							<template #icon><BlockIcon></BlockIcon></template>
							<template #title>Blocks added</template>
							<template #value>{{
								metricsData.blockMetrics.blocksAdded
							}}</template>
						</KeyValueChartCard>
					</KeepAlive>
				</div>
				<div class="w-full">
					<KeyValueChartCard
						:x-values="metricsData.blockMetrics.buckets.x_Time"
						:y-values="metricsData.blockMetrics.buckets.y_BlockTimeAvg"
					>
						<template #topRight
							><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
						/></template>
						<template #title>Avg block time</template>
						<template #icon><StopwatchIcon /></template>
						<template #value>{{
							metricsData.blockMetrics.avgBlockTime
						}}</template>
						<template #unit>s</template>
					</KeyValueChartCard>
				</div>
				<div class="w-full">
					<KeyValueChartCard
						:x-values="metricsData.blockMetrics.buckets.x_Time"
						:y-values="metricsData.blockMetrics.buckets.y_BlockTimeMin"
					>
						<template #topRight
							><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
						/></template>
						<template #icon><StopwatchIcon /></template>
						<template #title>Min block time</template>
						<template #value>{{
							metricsData.blockMetrics.buckets.y_BlockTimeMin[0]
						}}</template>
						<template #unit>s</template>
					</KeyValueChartCard>
				</div>
				<div class="w-full">
					<KeyValueChartCard
						:x-values="metricsData.blockMetrics.buckets.x_Time"
						:y-values="metricsData.blockMetrics.buckets.y_BlockTimeMax"
					>
						<template #topRight
							><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
						/></template>
						<template #icon><StopwatchIcon /></template>
						<template #title>Max block time</template>
						<template #value>{{
							metricsData.blockMetrics.buckets.y_BlockTimeMax[0]
						}}</template>
						<template #unit>s</template>
					</KeyValueChartCard>
				</div>
			</div>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="20%">Height</TableTh>
						<TableTh width="20%">Status</TableTh>
						<TableTh width="30%">Timestamp</TableTh>
						<TableTh width="10%">Block hash</TableTh>
						<TableTh width="10%">Baker</TableTh>
						<TableTh width="10%" align="right">Transactions</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow>
						<TableTd colspan="6" align="center" class="p-0 tdlol">
							<ShowMoreButton :new-item-count="newItems" :refetch="refetch" />
						</TableTd>
					</TableRow>
					<TableRow v-for="block in pagedData" :key="block.blockHash">
						<TableTd :class="$style.numerical">{{ block.blockHeight }}</TableTd>
						<TableTd>
							<StatusCircle
								:class="[
									'h-4 w-6 mr-2 text-theme-interactive',
									{ 'text-theme-info': !block.finalized },
								]"
							/>
							{{ block.finalized ? 'Finalised' : 'Pending' }}
						</TableTd>
						<TableTd>
							<Tooltip :text="block.blockSlotTime">
								{{ convertTimestampToRelative(block.blockSlotTime) }}
							</Tooltip>
						</TableTd>
						<TableTd>
							<LinkButton
								class="numerical"
								@click="
									() => {
										drawer.push('block', block.blockHash, block.id)
									}
								"
							>
								<BlockIcon :class="$style.cellIcon" />
								<Tooltip :text="block.blockHash" text-class="text-theme-body">
									{{ shortenHash(block.blockHash) }}
								</Tooltip>
							</LinkButton>
						</TableTd>
						<TableTd :class="$style.numerical">
							<UserIcon
								v-if="block.bakerId || block.bakerId === 0"
								:class="$style.cellIcon"
							/>
							{{ block.bakerId }}
						</TableTd>
						<TableTd align="right" :class="$style.numerical">
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
		</main>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import BlockIcon from '~/components/icons/BlockIcon.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import { convertTimestampToRelative, shortenHash } from '~/utils/format'
import { usePagedData } from '~/composables/usePagedData'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import type { Block, BlockSubscriptionResponse } from '~/types/blocks'
import { useDrawer } from '~/composables/useDrawer'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import { MetricsPeriod } from '~/types/generated'
import StopwatchIcon from '~/components/icons/StopwatchIcon.vue'
import MetricsPeriodDropdown from '~/components/molecules/MetricsPeriodDropdown.vue'

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
const subscriptionHandler = (
	_prevData: void,
	_newData: BlockSubscriptionResponse
) => {
	newItems.value++
}
const selectedMetricsPeriod = ref(MetricsPeriod.LastHour)
useBlockSubscription(subscriptionHandler)

const refetch = () => {
	fetchNew(newItems.value)
	newItems.value = 0
}

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

const drawer = useDrawer()
const { data: metricsData } = useBlockMetricsQuery(selectedMetricsPeriod)
</script>

<style module>
.cellIcon {
	@apply h-4 w-6 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>

<style>
.tdlol {
	padding-left: 0 !important;
	padding-right: 0 !important;
}
.hello {
	transition: background-color 0.2s ease-in;
}

.hello:hover {
	background-color: hsl(0deg 0% 83% / 6%);
}
.cardShadow {
	filter: drop-shadow(0px 24px 38px rgba(0, 0, 0, 0.14))
		drop-shadow(0px 9px 46px rgba(0, 0, 0, 0.12))
		drop-shadow(0px 11px 15px rgba(0, 0, 0, 0.2));
}
</style>
