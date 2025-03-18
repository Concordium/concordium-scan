<template>
	<div>
		<Title>CCDScan | Stable Coin</Title>
		<div class="">
			<div
				class="flex flex-row justify-center< lg:place-content-end mb-4 lg:mb-0"
			></div>
			<FtbCarousel non-carousel-classes="grid-cols-4">
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Total Market Cap</template>
						<template #value>{{ '$' + formatNumber(totalMarketCap) }}</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Unique Holders</template>
						<template #value>{{ uniqueHolders.size }}</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>No. of Txs</template>
						<template #value>{{ totalTransactions }}</template>
					</KeyValueChartCard>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<KeyValueChartCard
						class="w-96 lg:w-full"
						:y-values="[[null]]"
						:is-loading="false"
					>
						<template #title>Total Values Transferd</template>
						<template #value>{{
							'$' + formatNumber(totalValueTransferred)
						}}</template>
					</KeyValueChartCard>
				</CarouselSlide>
			</FtbCarousel>

			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinSupplyBarChart
						:stable-coins-data="stableCoinsDataAll"
						:is-loading="stableCoinsFetching"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinDistributionChart
						:stable-coins-data="stableCoinsDataAll"
						:is-loading="stableCoinsFetching"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { useStableCoinsQuery } from '~/queries/useStableCoinQuery'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinDistributionChart from '~/components/molecules/ChartCards/StableCoinDistributionChart.vue'
import StableCoinSupplyBarChart from '~/components/molecules/ChartCards/StableCoinSupplyBarChart.vue'
const { data: stableCoinsData, fetching: stableCoinsFetching } =
	useStableCoinsQuery()

let stableCoinsDataAll = ref([])
let totalMarketCap = ref(0)
let totalTransactions = ref(0)
let uniqueHolders = new Set<string>()
let totalValueTransferred = ref(0)
const formatNumber = (num: number): string => {
	if (num >= 1e12) return (num / 1e12).toFixed(2) + 'T' // Trillion
	if (num >= 1e9) return (num / 1e9).toFixed(2) + 'B' // Billion
	if (num >= 1e6) return (num / 1e6).toFixed(2) + 'M' // Million
	if (num >= 1e3) return (num / 1e3).toFixed(2) + 'K' // Thousand
	return num.toFixed(2) // Show full value if less than 1K
}

watch(
	stableCoinsData,
	newData => {
		stableCoinsDataAll = ref([])
		totalMarketCap = ref(0)
		totalTransactions = ref(0)
		uniqueHolders = new Set<string>()
		totalValueTransferred = ref(0)
		if (newData?.stablecoins == undefined) return
		const data = unref(toRaw(newData.stablecoins))

		// eslint-disable-next-line @typescript-eslint/no-explicit-any
		const stableCoinsData = data.map((item: any) => {
			if (item.transfers) {
				totalTransactions.value += item.transfers.length
				// eslint-disable-next-line @typescript-eslint/no-explicit-any
				item.transfers.forEach((tx: any) => {
					uniqueHolders.add(tx.from)
					uniqueHolders.add(tx.to)
					totalValueTransferred.value += Number(tx.amount.replace(/,/g, ''))
				})
			}

			totalMarketCap.value += Number(item.totalSupply.replace(/,/g, ''))
			return {
				name: item.symbol,
				value: item.totalSupply,
			}
		})
		stableCoinsDataAll.value = stableCoinsData
	},
	{ immediate: true }
)
</script>
