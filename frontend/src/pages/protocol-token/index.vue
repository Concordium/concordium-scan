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
						<template #value
							><p class="font-bold text-2xl mt-2">
								{{
									'$' +
									numberFormatter(
										overviewData?.stablecoinOverview?.totalMarketcap
									)
								}}
							</p></template
						>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Unique Holders</template>
						<template #value
							><p class="font-bold text-2xl mt-2">
								{{ overviewData?.stablecoinOverview?.numberOfUniqueHolders }}
							</p></template
						>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title># of Txs (24h)</template>
						<template #value
							><p class="font-bold text-2xl mt-2">
								{{ overviewData?.stablecoinOverview?.noOfTxnLast24H }}
							</p></template
						>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Total Values Transfer (24h)</template>
						<template #value
							><p class="font-bold text-2xl mt-2">
								{{
									'$' +
									numberFormatter(
										overviewData?.stablecoinOverview?.valuesTransferredLast24H
									)
								}}
							</p></template
						>
					</KeyValueChartCard>
				</CarouselSlide>
			</FtbCarousel>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Supply & Holders</h1>
			</header>
			<FtbCarousel non-carousel-classes="grid-cols-3">
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinSupplyBarChart :stable-coins-data="stableCoinsData" />
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinDistributionChart :stable-coins-data="stableCoinsData" />
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<HolderByStableCoin :stable-coins-data="stableCoinsData" />
				</CarouselSlide>
			</FtbCarousel>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Transactions</h1>
			</header>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="20%">Transaction Hash</TableTh>
						<TableTh width="20%">Age</TableTh>
						<TableTh width="20%">Token Name</TableTh>
						<TableTh width="20%">From</TableTh>
						<TableTh width="20%">To</TableTh>
						<TableTh width="20%">Amount</TableTh>
						<TableTh width="20%">Value</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow
						v-for="(coin, index) in latestTransactionsData?.latestTransactions"
						:key="index"
					>
						<TableTd>
							<TransactionLink :hash="coin.transactionHash" />
						</TableTd>
						<TableTd> 12 days ago </TableTd>

						<TableTd>
							<a
								:href="`/protocol-token/${coin.assetName.toLowerCase()}`"
								target="_blank"
								class="font-normal text-md text-theme-interactive flex flex-row items-center"
							>
								<img
									:src="coin.assetMetadata?.iconUrl"
									class="rounded-full w-6 h-6 mr-2"
									alt="Token Icon"
									loading="lazy"
									decoding="async"
								/>
								{{ coin.assetName }}
							</a></TableTd
						>
						<TableTd>
							<AccountLink :address="coin.from" />
						</TableTd>
						<TableTd>
							<AccountLink :address="coin.to" />
						</TableTd>
						<TableTd> {{ coin.amount?.toFixed(2) }} </TableTd>
						<TableTd>{{ coin.value?.toFixed(2) }} </TableTd>
					</TableRow>
				</TableBody>
			</Table>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { numberFormatter } from '~/utils/format'
import { useStablecoinOverviewQuery } from '~/queries/useStablecoinOverviewQuery'
import { useStableCoinsQuery } from '~/queries/useStableCoinQuery'
import { useStableCoinLatestTransactionsQuery } from '~/queries/useStableCoinLatestTransactionsQuery'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinDistributionChart from '~/components/molecules/ChartCards/StableCoinDistributionChart.vue'
import StableCoinSupplyBarChart from '~/components/molecules/ChartCards/StableCoinSupplyBarChart.vue'
import HolderByStableCoin from '~/components/molecules/ChartCards/HolderByStableCoin.vue'

definePageMeta({
	middleware: 'plt-features-guard',
})

const { data: stableCoinsData } = useStableCoinsQuery()

const { data: latestTransactionsData } = useStableCoinLatestTransactionsQuery(5)

const { data: overviewData } = useStablecoinOverviewQuery()
</script>
