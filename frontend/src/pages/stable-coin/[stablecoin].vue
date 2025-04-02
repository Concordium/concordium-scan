<template>
	<div>
		<Title>CCDScan | Stable Coin</Title>
		<header class="flex justify-between items-center mb-4">
			<h1 class="text-xl">Overview {{ coinId }}</h1>
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
							{{ formatNumber(dataTransferSummary?.stablecoin?.totalSupply) }}
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
					<template #title>Current Supply</template>
					<template #value
						><p class="font-bold text-2xl mt-2">
							{{ formatNumber(dataTransferSummary?.stablecoin?.totalSupply) }}
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
					<template #title># Holders</template>
					<template #value
						><p class="font-bold text-2xl mt-2">
							{{ dataTransferSummary?.stablecoin?.totalUniqueHolder }}
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
					<template #title>Price</template>
					<template #value
						><p class="font-bold text-2xl mt-2">
							${{ dataTransferSummary?.stablecoin?.valueInDoller }}
						</p></template
					>
				</KeyValueChartCard>
			</CarouselSlide>
		</FtbCarousel>

		<header
			class="flex flex-wrap justify-between gap-8 w-full mb-4 mt-8 lg:mt-0"
		>
			<div class="flex flex-wrap flex-grow items-center gap-8">
				<TabBar>
					<TabBarItem
						tab-id="holders"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Holders
					</TabBarItem>
					<TabBarItem
						tab-id="analytics"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Analytics
					</TabBarItem>
				</TabBar>
			</div>
		</header>
		<Holders v-if="selectedTab === 'holders'" />
		<Analytics v-else />
	</div>
</template>
<script lang="ts" setup>
import TabBar from '~/components/atoms/TabBar.vue'
import TabBarItem from '~/components/atoms/TabBarItem.vue'
import { useStableCoinTokenTransferQuery } from '~/queries/useStableCoinTokenTransferQuery'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import Holders from '~/components/StableCoin/Holders.vue'
import Analytics from '~/components/StableCoin/Analytics.vue'
import { useRoute } from 'vue-router'
const route = useRoute()

const coinId = computed(() => route.params.stablecoin)

watch(
	() => coinId.value,
	(newId, oldId) => {
		console.log(`User ID changed from ${oldId} to ${newId}`)
	}
)

const { data: dataTransferSummary } = useStableCoinTokenTransferQuery(
	'USDC',
	12
)

const selectedTab = ref('holders')
const handleSelectTab = (tabId: string) => (selectedTab.value = tabId)

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
