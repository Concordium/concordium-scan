﻿<template>
	<div>
		<Title>CCDScan | Dashboard</Title>
		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			>
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-3">
				<CarouselSlide class="w-full lg:col-span-2">
					<MetricCard class="pt-4">
						<TotalCCD
							:last-total-micro-ccd="
								blockMetricsData?.blockMetrics.lastTotalMicroCcd
							"
						/>
						<TotalCirculatingCCD
							:last-total-micro-ccd-released="
								blockMetricsData?.blockMetrics.lastTotalMicroCcdReleased
							"
						/>
					</MetricCard>
				</CarouselSlide>
				<CarouselSlide class="w-full"
					><BlocksAddedChart
						:block-metrics-data="blockMetricsData"
						:is-loading="blockMetricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<BlockTimeChart
						:block-metrics-data="blockMetricsData"
						:is-loading="blockMetricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full"
					><TransactionCountChart
						:transaction-metrics-data="transactionMetricsData"
						:is-loading="transactionMetricsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<AccountsCreatedChart
						:account-metrics-data="accountMetricsData"
						:is-loading="accountMetricsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
		<section class="flex flex-wrap gap-16">
			<article class="max-w-full flex flex-col flex-1 mb-12">
				<header class="flex justify-between items-center mb-4">
					<h1 class="text-xl">Latest blocks</h1>
					<NuxtLink to="/blocks">
						<Button title="Show all blocks">Show all</Button>
					</NuxtLink>
				</header>
				<TableSkeletonLoader v-if="pausedSubscriptions" />
				<Table v-else>
					<TableHead>
						<TableRow>
							<TableTh width="25%">Height</TableTh>
							<TableTh width="25%">Block hash</TableTh>
							<TableTh v-if="breakpoint >= Breakpoint.MD" width="25%"
								>Validator</TableTh
							>
							<TableTh
								v-if="breakpoint >= Breakpoint.SM"
								align="right"
								width="25%"
							>
								Transactions
							</TableTh>
						</TableRow>
					</TableHead>

					<TransitionGroup name="list" tag="tbody">
						<TableRow v-for="block in blocks" :key="block.blockHash">
							<TableTd class="numerical">
								<BlockFinalized
									:finalized="block.finalized"
									:show-text="false"
								/>
								<span>
									{{ block.blockHeight }}
								</span>
							</TableTd>
							<TableTd>
								<BlockLink :id="block.id" :hash="block.blockHash" />
							</TableTd>
							<TableTd v-if="breakpoint >= Breakpoint.MD" class="numerical">
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
					</TransitionGroup>
				</Table>
			</article>

			<article class="max-w-full flex flex-col flex-1 mb-12">
				<header class="flex justify-between items-center mb-4">
					<h1 class="text-xl">Latest transactions</h1>
					<NuxtLink to="/transactions">
						<Button title="Show all transactions">Show all</Button>
					</NuxtLink>
				</header>
				<TableSkeletonLoader v-if="pausedSubscriptions" />
				<Table v-else>
					<TableHead>
						<TableRow>
							<TableTh width="25%">Type</TableTh>
							<TableTh width="25%">Transaction hash</TableTh>
							<TableTh v-if="breakpoint >= Breakpoint.MD" width="25%"
								>Sender</TableTh
							>
							<TableTh
								v-if="breakpoint >= Breakpoint.MD"
								align="right"
								width="25%"
							>
								Cost (Ͼ)
							</TableTh>
						</TableRow>
					</TableHead>
					<TransitionGroup name="list" tag="tbody">
						<TableRow
							v-for="transaction in transactions"
							:key="transaction.transactionHash"
						>
							<TableTd>
								<div class="flex">
									<TransactionResult :result="transaction.result" />

									<div class="whitespace-normal lg:whitespace-nowrap">
										{{ translateTransactionType(transaction.transactionType) }}
									</div>
								</div>
							</TableTd>
							<TableTd>
								<TransactionLink
									:id="transaction.id"
									:hash="transaction.transactionHash"
								/>
							</TableTd>
							<TableTd v-if="breakpoint >= Breakpoint.MD" class="numerical">
								<AccountLink
									:address="transaction.senderAccountAddress?.asString"
								/>
							</TableTd>
							<TableTd
								v-if="breakpoint >= Breakpoint.MD"
								align="right"
								class="numerical"
							>
								<Amount :amount="transaction.ccdCost" />
							</TableTd>
						</TableRow>
					</TransitionGroup>
				</Table>
			</article>
		</section>
	</div>
</template>

<script lang="ts" setup>
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useTransactionsListQuery } from '~/queries/useTransactionListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import {
	MetricsPeriod,
	type Transaction,
	type Block,
	type Subscription,
} from '~/types/generated'
import { useTransactionMetricsQuery } from '~/queries/useTransactionMetrics'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import Amount from '~/components/atoms/Amount.vue'
import MetricCard from '~/components/atoms/MetricCard.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import BlockFinalized from '~/components/molecules/BlockFinalized.vue'
import TransactionResult from '~/components/molecules/TransactionResult.vue'
import TotalCCD from '~/components/molecules/ChartCards/TotalCCD.vue'
import TotalCirculatingCCD from '~/components/molecules/ChartCards/TotalCirculatingCCD.vue'
import BlocksAddedChart from '~/components/molecules/ChartCards/BlocksAddedChart.vue'
import BlockTimeChart from '~/components/molecules/ChartCards/BlockTimeChart.vue'
import TransactionCountChart from '~/components/molecules/ChartCards/TransactionCountChart.vue'
import AccountsCreatedChart from '~/components/molecules/ChartCards/AccountsCreatedChart.vue'
import TableSkeletonLoader from '~/components/Table/TableSkeletonLoader.vue'

const pageSize = 10
const queueSize = 10
const drawInterval = 1000 // in ms
let loopInterval: NodeJS.Timeout

const { breakpoint } = useBreakpoint()

const subscriptionHandler = (_prevData: unknown, newData: Subscription) => {
	if (newData && newData.blockAdded) {
		if (
			!blocksQueue.value.some(
				oldBlock => oldBlock.blockHash === newData.blockAdded.blockHash
			) &&
			!blocks.value.some(
				oldBlock => oldBlock.blockHash === newData.blockAdded.blockHash
			)
		) {
			if (blocksQueue.value.length === queueSize) blocksQueue.value.shift()
			blocksQueue.value.push(newData.blockAdded)
			if (
				newData.blockAdded.transactions &&
				newData.blockAdded.transactions.nodes
			) {
				for (let i = 0; i < newData.blockAdded.transactions.nodes.length; i++) {
					if (transactionsQueue.value.length === queueSize)
						transactionsQueue.value.shift()
					transactionsQueue.value.push(newData.blockAdded.transactions.nodes[i])
				}
			}
		}
	}
}

const { pause: pauseSubscription, resume: resumeSubscription } =
	useBlockSubscription(subscriptionHandler)

onUnmounted(() => {
	if (loopInterval) clearInterval(loopInterval)
	blocks.value = []
	transactions.value = []
	blocksQueue.value = []
	transactionsQueue.value = []
	window.onblur = null
	window.onfocus = null
})
const blocks = ref<Block[]>([])
const transactions = ref<Transaction[]>([])
const blocksQueue = ref<Block[]>([])
const transactionsQueue = ref<Transaction[]>([])
const { data: blockData, executeQuery: blockRefetch } = useBlockListQuery({
	first: pageSize,
})
const { data: txData, executeQuery: transactionsRefetch } =
	useTransactionsListQuery({ first: pageSize })
const pausedSubscriptions = ref(false)
onMounted(() => {
	resumeSubscription()
	drawFunc()
	if (window) {
		window.onblur = () => {
			if (loopInterval) clearTimeout(loopInterval)
			// pausedSubscriptions.value = true
			pauseSubscription()
		}
		window.onfocus = () => {
			blockRefetch({
				requestPolicy: 'network-only',
			})
			transactionsRefetch({
				requestPolicy: 'network-only',
			})
			pausedSubscriptions.value = false
			resumeSubscription()
			drawFunc()
		}
	}
})
const selectedMetricsPeriod = ref(MetricsPeriod.Last30Days)
const { data: accountMetricsData, fetching: accountMetricsFetching } =
	useAccountsMetricsQuery(selectedMetricsPeriod)
const { data: transactionMetricsData, fetching: transactionMetricsFetching } =
	useTransactionMetricsQuery(selectedMetricsPeriod)
const { data: blockMetricsData, fetching: blockMetricsFetching } =
	useBlockMetricsQuery(selectedMetricsPeriod)

watch(blockData, () => {
	if (blockData && blockData.value)
		blocks.value = blockData.value.blocks.nodes.slice(0, pageSize)
})
watch(txData, () => {
	if (txData && txData.value)
		transactions.value = txData.value.transactions.nodes.slice(0, pageSize)
})
const loadInitialValuesIfEmpty = () => {
	if (
		blocks.value.length === 0 &&
		blockData &&
		blockData.value &&
		blockData.value.blocks
	)
		blocks.value = blockData.value.blocks.nodes.slice(0, pageSize)
	if (
		transactions.value.length === 0 &&
		txData &&
		txData.value &&
		txData.value?.transactions
	)
		transactions.value = txData.value.transactions.nodes.slice(0, pageSize)
}
const drawFunc = () => {
	loadInitialValuesIfEmpty()
	for (let i = 0; i < pageSize; i++) {
		if (blocksQueue.value.length > 0) {
			let blockAdded = false

			while (!blockAdded && blocksQueue.value.length > 0) {
				const nextBlock = blocksQueue.value.shift() as Block
				if (
					blocks.value.some(
						oldBlock => oldBlock.blockHash === nextBlock.blockHash
					)
				)
					continue
				if (blocks.value.length >= pageSize) blocks.value.pop()
				blocks.value.unshift(nextBlock)
				blockAdded = true
			}
		}

		if (transactionsQueue.value.length > 0) {
			let transactionAdded = false
			while (!transactionAdded && transactionsQueue.value.length > 0) {
				const nextTransaction = transactionsQueue.value.shift() as Transaction
				if (
					transactions.value.some(
						oldTransaction =>
							oldTransaction.transactionHash === nextTransaction.transactionHash
					)
				)
					continue
				if (transactions.value.length >= pageSize) transactions.value.pop()
				transactions.value.unshift(nextTransaction)
				transactionAdded = true
			}
		}
	}
	loopInterval = setTimeout(drawFunc, drawInterval)
}
</script>

<style module>
.list-move,
.list-enter-active,
.list-leave-active {
	transition: transform 0.2s ease-out;
}

.list-enter-from,
.list-leave-to {
	transform: translateY(-30px);
	opacity: 0;
}

.list-leave-active {
	transition: transform 0s;
	position: absolute;
	opacity: 0;
}
</style>
