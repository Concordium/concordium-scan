<template>
	<div>
		<Title>CCDScan | Stable Coin</Title>
		<FtbCarousel non-carousel-classes="grid-cols-2">
			<CarouselSlide class="w-full lg:h-full">
				<div
					class="flex flex-col p-8 h-full w-full my-4 relative cardShadow rounded-2xl shadow-2xl overflow-hidden bg-theme-background-primary-elevated"
				>
					<h1>Market Overview</h1>
					<div class="flex flex-col">
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Price</p>
							<p class="font-bold text-xl text-theme-interactive">
								${{ dataTransferSummary?.stablecoin?.valueInDoller }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Market Cap</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ formatNumber(dataTransferSummary?.stablecoin?.totalSupply) }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Current Supply</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ formatNumber(dataTransferSummary?.stablecoin?.totalSupply) }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded"># Holder</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ dataTransferSummary?.stablecoin?.totalUniqueHolder }}
							</p>
						</div>
					</div>
				</div>
			</CarouselSlide>
			<CarouselSlide class="w-full lg:h-full">
				<div
					class="flex flex-col p-8 h-full w-full my-4 relative cardShadow rounded-2xl shadow-2xl overflow-hidden bg-theme-background-primary-elevated"
				>
					<h1>Profile Summary</h1>
					<div class="flex flex-col">
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Token name</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ dataTransferSummary?.stablecoin?.name }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Symbol</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ dataTransferSummary?.stablecoin?.symbol }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Decimals</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ dataTransferSummary?.stablecoin?.decimal }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Issuer</p>
							<p class="font-bold text-xl text-theme-interactive"></p>
						</div>
					</div>
				</div>
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
		<Holders v-if="selectedTab === 'holders'" :coin-id="coinId" />
		<Analytics v-else :coin-id="coinId" />
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
	coinId.value.toUpperCase(),
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
