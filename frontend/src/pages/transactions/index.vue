<template>
	<div>
		<Title>CCDScan | Transactions</Title>
		<FtbCarousel>
			<CarouselSlide class="w-full">
				<KeyValueChartCard
					:x-values="metricsData?.transactionMetrics?.buckets?.x_Time"
					:bucket-width="metricsData?.transactionMetrics?.buckets?.bucketWidth"
					:y-values="
						metricsData?.transactionMetrics?.buckets
							?.y_LastCumulativeTransactionCount
					"
				>
					<template #topRight
						><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
					/></template>
					<template #title>Cumulative Transactions</template>
					<template #icon><TransactionIcon /></template>
					<template #value>{{
						formatNumber(
							metricsData?.transactionMetrics?.lastCumulativeTransactionCount
						)
					}}</template>
					<template #chip>latest</template>
				</KeyValueChartCard>
			</CarouselSlide>
			<CarouselSlide class="w-full">
				<KeyValueChartCard
					:x-values="metricsData?.transactionMetrics?.buckets?.x_Time"
					:bucket-width="metricsData?.transactionMetrics?.buckets?.bucketWidth"
					:y-values="
						metricsData?.transactionMetrics?.buckets?.y_TransactionCount
					"
				>
					<template #topRight
						><MetricsPeriodDropdown v-model="selectedMetricsPeriod"
					/></template>
					<template #title>Transactions</template>
					<template #icon><TransactionIcon /></template>
					<template #value>{{
						formatNumber(metricsData?.transactionMetrics?.transactionCount)
					}}</template>
					<template #chip>sum</template>
				</KeyValueChartCard>
			</CarouselSlide>
		</FtbCarousel>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="10%">Status</TableTh>
					<TableTh width="20%">Timestamp</TableTh>
					<TableTh width="30%">Type</TableTh>
					<TableTh width="10%">Transaction hash</TableTh>
					<TableTh width="10%">Block height</TableTh>
					<TableTh width="10%">Sender</TableTh>
					<TableTh width="10%" align="right">Cost (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow>
					<TableTd colspan="7" align="center" class="p-0 tdlol">
						<ShowMoreButton :new-item-count="newItems" :refetch="refetch" />
					</TableTd>
				</TableRow>
				<TableRow
					v-for="transaction in pagedData"
					:key="transaction.transactionHash"
				>
					<TableTd>
						<StatusCircle
							:class="[
								'h-4 w-6 mr-2 text-theme-interactive',
								{
									'text-theme-error':
										transaction.result.__typename === 'Rejected',
								},
							]"
						/>
						{{
							transaction.result.__typename === 'Success'
								? 'Success'
								: 'Rejected'
						}}
					</TableTd>
					<TableTd>
						<Tooltip :text="transaction.block.blockSlotTime">
							{{ convertTimestampToRelative(transaction.block.blockSlotTime) }}
						</Tooltip>
					</TableTd>
					<TableTd>
						{{ translateTransactionType(transaction.transactionType) }}
					</TableTd>
					<TableTd>
						<TransactionLink
							:id="transaction.id"
							:hash="transaction.transactionHash"
						/>
					</TableTd>
					<TableTd :class="$style.numerical">
						{{ transaction.block.blockHeight }}
					</TableTd>
					<TableTd :class="$style.numerical">
						<AccountLink :address="transaction.senderAccountAddress" />
					</TableTd>
					<TableTd align="right" :class="$style.numerical">
						{{ convertMicroCcdToCcd(transaction.ccdCost) }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<LoadMore
			v-if="data?.transactions.pageInfo"
			:page-info="data?.transactions.pageInfo"
			:on-load-more="loadMore"
		/>
	</div>
</template>

<script lang="ts" setup>
import Tooltip from '~/components/atoms/Tooltip.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import {
	formatNumber,
	convertMicroCcdToCcd,
	convertTimestampToRelative,
} from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { usePagedData } from '~/composables/usePagedData'
import { useTransactionsListQuery } from '~/queries/useTransactionListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import type { BlockSubscriptionResponse } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'
import { useTransactionMetricsQuery } from '~/queries/useTransactionMetrics'
import { MetricsPeriod } from '~/types/generated'
const {
	pagedData,
	first,
	last,
	after,
	before,
	addPagedData,
	fetchNew,
	loadMore,
} = usePagedData<Transaction>()
const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const newItems = ref(0)
const subscriptionHandler = (
	_prevData: void,
	newData: BlockSubscriptionResponse
) => {
	newItems.value += newData.blockAdded.transactionCount
}

const { pause: pauseSubscription, resume: resumeSubscription } =
	useBlockSubscription(subscriptionHandler)
onMounted(() => {
	resumeSubscription()
})
onUnmounted(() => {
	pauseSubscription()
})
const refetch = () => {
	fetchNew(newItems.value)
	newItems.value = 0
}

const { data } = useTransactionsListQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.transactions.nodes || [], value?.transactions.pageInfo)
	}
)
const { data: metricsData } = useTransactionMetricsQuery(selectedMetricsPeriod)
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}

.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
