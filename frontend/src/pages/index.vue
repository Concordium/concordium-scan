<template>
	<div>
		<Title>CCDScan | Dashboard</Title>
		<div class="">
			<div class="flex flex-row justify-center lg:place-content-end">
				<MetricsPeriodDropdown v-model="selectedMetricsPeriod" />
			</div>
			<FtbCarousel non-carousel-classes="grid-cols-4">
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
						:y-values="blockMetricsData?.blockMetrics?.buckets?.y_BlocksAdded"
						:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
					>
						<template #topRight></template>
						<template #icon><BlockIcon></BlockIcon></template>
						<template #title>Blocks added</template>
						<template #value>{{
							formatNumber(blockMetricsData?.blockMetrics?.blocksAdded)
						}}</template>
						<template #chip>latest</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="blockMetricsData?.blockMetrics?.buckets?.x_Time"
						:bucket-width="blockMetricsData?.blockMetrics?.buckets?.bucketWidth"
						chart-type="area"
						:y-values="[
							blockMetricsData?.blockMetrics?.buckets?.y_BlockTimeMax,
							blockMetricsData?.blockMetrics?.buckets?.y_BlockTimeAvg,
							blockMetricsData?.blockMetrics?.buckets?.y_BlockTimeMin,
						]"
					>
						<template #topRight></template>
						<template #title>Block time</template>
						<template #icon><StopwatchIcon /></template>
						<template #value>{{
							blockMetricsData?.blockMetrics?.avgBlockTime
						}}</template>
						<template #unit>s</template>
						<template #chip>average</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="
							transactionMetricsData?.transactionMetrics?.buckets?.x_Time
						"
						:bucket-width="
							transactionMetricsData?.transactionMetrics?.buckets?.bucketWidth
						"
						:y-values="
							transactionMetricsData?.transactionMetrics?.buckets
								?.y_TransactionCount
						"
					>
						<template #topRight></template>
						<template #title>Transactions</template>
						<template #icon><TransactionIcon /></template>
						<template #value>{{
							formatNumber(
								transactionMetricsData?.transactionMetrics?.transactionCount
							)
						}}</template>
						<template #chip>sum</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:x-values="accountMetricsData?.accountsMetrics?.buckets?.x_Time"
						:y-values="
							accountMetricsData?.accountsMetrics?.buckets?.y_AccountsCreated
						"
						:bucket-width="
							accountMetricsData?.accountsMetrics?.buckets?.bucketWidth
						"
					>
						<template #topRight></template>
						<template #title>Accounts Created</template>
						<template #icon><UserIcon /></template>
						<template #chip>sum</template>
						<template #value>{{
							formatNumber(accountMetricsData?.accountsMetrics?.accountsCreated)
						}}</template>
					</KeyValueChartCard>
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
				<Table>
					<TableHead>
						<TableRow>
							<TableTh>Height</TableTh>
							<TableTh>Block hash</TableTh>
							<TableTh>Baker</TableTh>
							<TableTh align="right">Baker reward (Ͼ)</TableTh>
						</TableRow>
					</TableHead>

					<TransitionGroup name="list" tag="tbody">
						<TableRow v-for="block in blocks" :key="block.blockHash">
							<TableTd class="numerical">
								<StatusCircle
									:class="[
										'h-4 mr-2 text-theme-interactive',
										{ 'text-theme-info': !block.finalized },
									]"
								/>
								{{ block.blockHeight }}
							</TableTd>
							<TableTd>
								<BlockLink :id="block.id" :hash="block.blockHash" />
							</TableTd>
							<TableTd class="numerical">
								<UserIcon
									v-if="block.bakerId || block.bakerId === 0"
									class="h-4 text-theme-white inline align-baseline"
								/>
								{{ block.bakerId }}
							</TableTd>
							<TableTd align="right" class="numerical">
								{{
									convertMicroCcdToCcd(
										block.specialEvents.blockRewards?.bakerReward
									)
								}}
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
				<Table>
					<TableHead>
						<TableRow>
							<TableTh>Type</TableTh>
							<TableTh>Transaction hash</TableTh>
							<TableTh>Sender</TableTh>
							<TableTh align="right">Cost (Ͼ)</TableTh>
						</TableRow>
					</TableHead>
					<TableBody>
						<TableRow
							v-for="transaction in transactions"
							:key="transaction.transactionHash"
						>
							<TableTd>
								<StatusCircle
									:class="[
										'h-4 mr-2 text-theme-interactive',
										{
											'text-theme-error':
												transaction.result.__typename === 'Rejected',
										},
									]"
								/>
								{{ translateTransactionType(transaction.transactionType) }}
							</TableTd>
							<TableTd>
								<TransactionLink
									:id="transaction.id"
									:hash="transaction.transactionHash"
								/>
							</TableTd>
							<TableTd class="numerical">
								<AccountLink :address="transaction.senderAccountAddress" />
							</TableTd>
							<TableTd align="right" class="numerical">
								{{ convertMicroCcdToCcd(transaction.ccdCost) }}
							</TableTd>
						</TableRow>
					</TableBody>
				</Table>
			</article>
		</section>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import BlockIcon from '~/components/icons/BlockIcon.vue'
import { useBlockListQuery } from '~/queries/useBlockListQuery'
import { useTransactionsListQuery } from '~/queries/useTransactionListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import { convertMicroCcdToCcd, formatNumber } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import type { BlockSubscriptionResponse, Block } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'
import { useAccountsMetricsQuery } from '~/queries/useAccountsMetricsQuery'
import { MetricsPeriod } from '~/types/generated'
import { useTransactionMetricsQuery } from '~/queries/useTransactionMetrics'
import { useBlockMetricsQuery } from '~/queries/useChartBlockMetrics'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
const pageSize = 10
const queueSize = 50
const drawInterval = 200 // in ms
let loopInterval: NodeJS.Timeout

const subscriptionHandler = (
	_prevData: void,
	newData: BlockSubscriptionResponse
) => {
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
		for (let i = 0; i < newData.blockAdded.transactions.nodes.length; i++) {
			if (transactionsQueue.value.length === queueSize)
				transactionsQueue.value.shift()
			transactionsQueue.value.push(newData.blockAdded.transactions.nodes[i])
		}
	}
}

const { pause: pauseSubscription, resume: resumeSubscription } =
	useBlockSubscription(subscriptionHandler)
onMounted(() => {
	resumeSubscription()
	loopInterval = setInterval(drawFunc, drawInterval)
})
onUnmounted(() => {
	pauseSubscription()
	clearInterval(loopInterval)
	blocks.value = []
	transactions.value = []
	blocksQueue.value = []
	transactionsQueue.value = []
})
const blocks = ref<Block[]>([])
const transactions = ref<Transaction[]>([])
const blocksQueue = ref<Block[]>([])
const transactionsQueue = ref<Transaction[]>([])
const { data: blockData } = useBlockListQuery({ first: pageSize })
const { data: txData } = useTransactionsListQuery({ first: pageSize })

const selectedMetricsPeriod = ref(MetricsPeriod.Last7Days)
const { data: accountMetricsData } = useAccountsMetricsQuery(
	selectedMetricsPeriod
)
const { data: transactionMetricsData } = useTransactionMetricsQuery(
	selectedMetricsPeriod
)
const { data: blockMetricsData } = useBlockMetricsQuery(selectedMetricsPeriod)
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
</script>

<style>
.list-move,
.list-enter-active,
.list-leave-active {
	transition: all 0.5s ease;
}

.list-enter-from,
.list-leave-to {
	opacity: 0;
	transform: translateY(-30px);
}

.list-leave-active {
	position: absolute;
}
</style>
