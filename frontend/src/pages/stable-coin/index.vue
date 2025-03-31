<template>
	<div>
		<Title>CCDScan | Stable Coin</Title>
		<div class="">
			<div
				class="flex flex-row justify-center< lg:place-content-end mb-4 lg:mb-0"
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
						<template #value>{{
							'$' +
							formatNumber(overviewData?.stablecoinOverview?.totalMarketcap)
						}}</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Unique Holder</template>
						<template #value>{{
							overviewData?.stablecoinOverview?.numberOfUniqueHolder
						}}</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>No. of Txs Transfer 24h</template>
						<template #value>{{
							overviewData?.stablecoinOverview?.noOfTxnLast24H
						}}</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Total Values Transfer 24h</template>
						<template #value>{{
							'$' +
							formatNumber(
								overviewData?.stablecoinOverview?.valuesTransferdLast24H
							)
						}}</template>
					</KeyValueChartCard>
				</CarouselSlide>
			</FtbCarousel>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Supply</h1>
			</header>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinSupplyBarChart
						:stable-coins-data="stableCoinsData"
						chart-type="supply"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinDistributionChart :stable-coins-data="stableCoinsData" />
				</CarouselSlide>
			</FtbCarousel>
			<header class="flex justify-between items-center mb-4">
				<h1 class="text-xl">Holder</h1>
			</header>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full lg:h-full">
					<HolderByStableCoin :stable-coins-data="stableCoinsData" />
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { useStablecoinOverviewQuery } from '~/queries/useStablecoinOverviewQuery'
import { useStableCoinsQuery } from '~/queries/useStableCoinQuery'

import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinDistributionChart from '~/components/molecules/ChartCards/StableCoinDistributionChart.vue'
import StableCoinSupplyBarChart from '~/components/molecules/ChartCards/StableCoinSupplyBarChart.vue'
import HolderByStableCoin from '~/components/molecules/ChartCards/HolderByStableCoin.vue'

const { data: stableCoinsData } = useStableCoinsQuery()

const { data: overviewData } = useStablecoinOverviewQuery()

const formatNumber = (num?: number): string => {
	if (typeof num !== 'number' || isNaN(num)) return '0'
	return num >= 1e12
		? (num / 1e12).toFixed(2) + 'T'
		: num >= 1e9
		? (num / 1e9).toFixed(2) + 'B'
		: num >= 1e6
		? (num / 1e6).toFixed(2) + 'M'
		: num >= 1e3
		? (num / 1e3).toFixed(2) + 'K'
		: num.toFixed(2)
}
</script>
