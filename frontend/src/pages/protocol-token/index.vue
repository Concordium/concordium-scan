<template>
	<div>
		<Title>CCDScan | Stable Coin</Title>

		<div class="">
			<div
				class="flex flex-row justify-center lg:place-content-end mb-4 lg:mb-0"
			></div>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Overview</h1>
			</header>
			<FtbCarousel non-carousel-classes="grid-cols-4">
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Total Market Cap</template>
						<template #value>
							<p class="font-bold text-2xl mt-2">
								{{
									numberFormatter(
										pltEventMetricsDataRef?.pltEventMetrics
											.lastCumulativeTotalSupply
									)
								}}
							</p>
						</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="pltEventMetricsLoading"
					>
						<template #title>Unique Holders</template>
						<template #value>
							<p class="font-bold text-2xl mt-2">
								{{
									pltEventMetricsDataRef?.pltEventMetrics
										.lastCumulativeUniqueHolders
								}}
							</p>
						</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="pltEventMetricsLoading"
					>
						<template #title># of Txs (24h)</template>
						<template #value>
							<p class="font-bold text-2xl mt-2">
								{{ pltEventMetricsDataRef?.pltEventMetrics.eventCount }}
							</p>
						</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="pltEventMetricsLoading"
					>
						<template #title>Total Values Transfer (24h)</template>
						<template #value>
							<p class="font-bold text-2xl mt-2">
								{{ '$' + 0.0 }}
							</p>
						</template>
					</KeyValueChartCard>
				</CarouselSlide>
			</FtbCarousel>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Supply & Holders</h1>
			</header>
			<FtbCarousel non-carousel-classes="grid-cols-3">
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinSupplyBarChart
						:stable-coins-data="pltTokenDataRef"
						:is-loading="pltTokenLoading"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinDistributionChart
						:stable-coins-data="pltTokenDataRef"
						:is-loading="pltTokenLoading"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<HolderByStableCoin
						:stable-coins-data="pltTokenDataRef"
						:is-loading="pltTokenLoading"
					/>
				</CarouselSlide>
			</FtbCarousel>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Transactions</h1>
			</header>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="12.5%">Transaction Hash</TableTh>
						<TableTh width="12.5%">Age</TableTh>
						<TableTh width="12.5%">Token Event</TableTh>
						<TableTh width="12.5%">Token Name</TableTh>
						<TableTh width="12.5%">From</TableTh>
						<TableTh width="12.5%">To</TableTh>
						<TableTh width="12.5%">Target</TableTh>
						<TableTh width="12.5%">Amount</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="(event, index) in pagedData" :key="index">
						<TableTd>
							<TransactionLink :hash="event.transactionHash ?? ''" />
						</TableTd>
						<TableTd>
							<Tooltip :text="formatTimestamp(event.block.blockSlotTime)">
								{{ convertTimestampToRelative(event.block.blockSlotTime, NOW) }}
							</Tooltip>
						</TableTd>
						<TableTd>
							<span class="text-theme-interactive font-semibold">
								{{
									event.eventType == 'TOKEN_MODULE'
										? event.tokenModuleType
										: event.eventType
								}}
							</span>
						</TableTd>

						<TableTd>
							<!-- <a
								:href="`/protocol-token/${event.tokenId}`"
								target="_blank"
								class="font-normal text-md text-theme-interactive flex flex-row items-center"
							>
								<img :src="event.tokenIconUrl" class="rounded-full w-6 h-6 mr-2"
									alt="Token Icon" loading="lazy" decoding="async" />
							</a> -->
							{{ event.tokenName }}
						</TableTd>
						<TableTd>
							<AccountLink
								:address="
									event.tokenEvent.__typename == 'TokenTransferEvent'
										? event.tokenEvent.from.address.asString
										: ''
								"
							/>
						</TableTd>
						<TableTd>
							<AccountLink
								:address="
									event.tokenEvent.__typename == 'TokenTransferEvent'
										? event.tokenEvent.to.address.asString
										: ''
								"
							/>
						</TableTd>
						<TableTd>
							<AccountLink
								:address="
									event.tokenEvent.__typename == 'BurnEvent' ||
									event.tokenEvent.__typename == 'MintEvent'
										? event.tokenEvent.target.address.asString
										: ''
								"
							/>
						</TableTd>
						<TableTd
							v-if="
								event.tokenEvent.__typename == 'BurnEvent' ||
								event.tokenEvent.__typename == 'MintEvent' ||
								event.tokenEvent.__typename == 'TokenTransferEvent'
							"
						>
							{{
								(
									Number(event.tokenEvent.amount.value) /
									Math.pow(10, event.tokenEvent.amount.decimals)
								).toFixed(event.tokenEvent.amount.decimals)
							}}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
			<LoadMore
				v-if="pltEventsDataRef?.pltEvents.pageInfo"
				:page-info="pltEventsDataRef?.pltEvents.pageInfo"
				:on-load-more="loadMore"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { ref } from 'vue'

import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinDistributionChart from '~/components/molecules/ChartCards/StableCoinDistributionChart.vue'
import StableCoinSupplyBarChart from '~/components/molecules/ChartCards/StableCoinSupplyBarChart.vue'
import { usePltTokenQuery } from '~/queries/usePltTokenQuery'
import { usePagedData } from '~/composables/usePagedData'
import { usePltEventsQuery } from '~/queries/usePltEventsQuery'
import type { Pltevent } from '~/types/generated'
import { useDateNow } from '~/composables/useDateNow'
import HolderByStableCoin from '~/components/molecules/ChartCards/HolderByStableCoin.vue'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import { usePltEventsMetricsQuery } from '~/queries/usePltEventsMetricsQuery'
import { MetricsPeriod } from '~/types/generated'
definePageMeta({
	middleware: 'plt-features-guard',
})

// page size and max page size for pagination plt events
const pageSize = 10
const maxPageSize = 20
const { pagedData, first, last, after, before, addPagedData, loadMore } =
	usePagedData<Pltevent>([], pageSize, maxPageSize)

const { data: pltTokenData, loading: pltTokenLoading } = usePltTokenQuery()

const { NOW } = useDateNow()

const pltTokenDataRef = ref(pltTokenData)
const selectedMetricsPeriod = ref(MetricsPeriod.Last24Hours)

watch(
	pltTokenData,
	newData => {
		pltTokenDataRef.value = newData
	},
	{ immediate: true, deep: true }
)

const { data: pltEventsData } = usePltEventsQuery({
	first,
	last,
	after,
	before,
})
const pltEventsDataRef = ref(pltEventsData)

watch(
	() => pltEventsDataRef.value,
	value => {
		addPagedData(value?.pltEvents.nodes || [], value?.pltEvents.pageInfo)
	},
	{ immediate: true, deep: true }
)

const { data: pltEventMetricsData, loading: pltEventMetricsLoading } =
	usePltEventsMetricsQuery(selectedMetricsPeriod)
const pltEventMetricsDataRef = ref(pltEventMetricsData)

watch(
	pltEventMetricsData,
	newData => {
		pltEventMetricsDataRef.value = newData
	},
	{ immediate: true, deep: true }
)
</script>
