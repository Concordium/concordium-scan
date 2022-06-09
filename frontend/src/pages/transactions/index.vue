<template>
	<div>
		<Title>CCDScan | Transactions</Title>
		<div>
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full">
					<MetricCard class="pt-4">
						<CumulativeTransactionsChart
							:transaction-metrics-data="metricsData"
							:is-loading="metricsFetching"
						/>
					</MetricCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<TransactionCountChart
						:transaction-metrics-data="metricsData"
						:is-loading="metricsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh width="10%">Transaction hash</TableTh>
					<TableTh width="10%">Status</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG" width="20%">
						Age
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.MD" width="30%">Type</TableTh>
					<TableTh width="10%">Block height</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XL" width="10%">
						Sender
					</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG" width="10%" align="right">
						Cost (Ͼ)
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow>
					<TableTd colspan="7" align="center" class="p-0">
						<ShowMoreButton :new-item-count="newItems" :refetch="refetch" />
					</TableTd>
				</TableRow>
				<TableRow
					v-for="transaction in pagedData"
					:key="transaction.transactionHash"
				>
					<TableTd>
						<TransactionLink
							:id="transaction.id"
							:hash="transaction.transactionHash"
						/>
					</TableTd>
					<TableTd>
						<TransactionResult :result="transaction.result" :show-text="true" />
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<Tooltip :text="formatTimestamp(transaction.block.blockSlotTime)">
							{{
								convertTimestampToRelative(transaction.block.blockSlotTime, NOW)
							}}
						</Tooltip>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.MD">
						<div class="whitespace-normal">
							{{ translateTransactionType(transaction.transactionType) }}
						</div>
					</TableTd>
					<TableTd class="numerical">
						{{ transaction.block.blockHeight }}
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XL" class="numerical">
						<AccountLink
							:address="transaction.senderAccountAddress?.asString"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						align="right"
						class="numerical"
					>
						<Amount :amount="transaction.ccdCost" />
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
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useDateNow } from '~/composables/useDateNow'
import { usePagedData } from '~/composables/usePagedData'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { useTransactionsListQuery } from '~/queries/useTransactionListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import { useTransactionMetricsQuery } from '~/queries/useTransactionMetrics'
import {
	MetricsPeriod,
	type Transaction,
	type Subscription,
} from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import MetricCard from '~/components/atoms/MetricCard.vue'
import TransactionResult from '~/components/molecules/TransactionResult.vue'
import TransactionCountChart from '~/components/molecules/ChartCards/TransactionCountChart.vue'
import CumulativeTransactionsChart from '~/components/molecules/ChartCards/CumulativeTransactionsChart.vue'

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
} = usePagedData<Transaction>()
const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const newItems = ref(0)
const subscriptionHandler = (_prevData: void, newData: Subscription) => {
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
const { data: metricsData, fetching: metricsFetching } =
	useTransactionMetricsQuery(selectedMetricsPeriod)
</script>
