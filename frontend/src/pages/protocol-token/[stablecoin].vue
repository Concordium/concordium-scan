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
						<!-- <div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Price</p>
							<p class="font-bold text-xl text-theme-interactive">
								${{ dataTransferSummary?.stablecoin?.valueInDollar }}
							</p>
						</div> -->
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Initial Supply</p>
							<p class="font-bold text-xl text-theme-interactive">
								<PltAmount
									:value="(pltTokenDataRef?.initialSupply ?? 0).toString()"
									:decimals="pltTokenDataRef?.decimal ?? 0"
									:format-number="true"
								/>
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Current Supply</p>
							<p class="font-bold text-xl text-theme-interactive">
								<PltAmount
									:value="(pltTokenDataRef?.totalSupply ?? 0).toString()"
									:decimals="pltTokenDataRef?.decimal ?? 0"
									:format-number="true"
								/>
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded"># of Holders</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ pltTokenDataRef?.totalUniqueHolders }}
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
						<!-- <div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Token name</p>
							<p
								class="font-bold text-xl text-theme-interactive flex flex-row items-center"
							>
								<img
									:src="dataTransferSummary?.stablecoin?.metadata?.iconUrl"
									class="rounded-full w-6 h-6 mr-2"
									alt="Token Icon"
									loading="lazy"
									decoding="async"
								/>
								{{ dataTransferSummary?.stablecoin?.name }}
							</p>
						</div> -->
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Name</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ pltTokenDataRef?.name }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Symbol</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ pltTokenDataRef?.tokenId }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Decimals</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ pltTokenDataRef?.decimal }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Issuer</p>
							<p
								class="font-bold text-xl text-theme-interactive flex flex-row items-center"
							>
								<AccountLink :address="pltTokenDataRef?.issuer.asString" />
							</p>
						</div>
					</div>
				</div>
			</CarouselSlide>
		</FtbCarousel>
		<header
			class="flex flex-wrap justify-between gap-8 w-full mb-4 mt-10 lg:mt-0"
		>
			<div class="flex flex-wrap flex-grow items-center gap-8">
				<TabBar>
					<TabBarItem
						tab-id="transactions"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Transactions
					</TabBarItem>
					<TabBarItem
						tab-id="holders"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Holders
					</TabBarItem>
					<!-- <TabBarItem
						tab-id="analytics"
						:selected-tab="selectedTab"
						:on-click="handleSelectTab"
					>
						Analytics
					</TabBarItem> -->
				</TabBar>
			</div>
		</header>
		<Holders
			v-if="selectedTab === 'holders'"
			:coin-id="coinId"
			:total-supply="BigInt(pltTokenDataRef?.totalSupply ?? 0)"
		/>
		<Transactions
			v-else-if="selectedTab === 'transactions'"
			:coin-id="coinId"
		/>
		<!-- <Analytics v-else :coin-id="coinId" /> -->
	</div>
</template>
<script lang="ts" setup>
import { computed, ref } from 'vue'
import TabBar from '~/components/atoms/TabBar.vue'
import TabBarItem from '~/components/atoms/TabBarItem.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import Holders from '~/components/StableCoin/Holders.vue'
import Transactions from '~/components/StableCoin/Transactions.vue'
import { usePltTokenQueryById } from '~/queries/usePltTokenQuery'

import { useRoute } from 'vue-router'

const route = useRoute()

definePageMeta({
	middleware: 'plt-features-guard',
})

const coinId = computed(() => {
	const id = route.params.stablecoin
	return Array.isArray(id) ? id[0] : id || ''
})

const { data: pltTokenData } = usePltTokenQueryById(coinId.value)
const pltTokenDataRef = ref(pltTokenData)

watch(
	pltTokenData,
	newData => {
		pltTokenDataRef.value = newData
	},
	{ immediate: true, deep: true }
)

const selectedTab = ref('transactions')
const handleSelectTab = (tabId: string) => (selectedTab.value = tabId)
</script>
