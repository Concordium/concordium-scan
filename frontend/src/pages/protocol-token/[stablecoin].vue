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
								${{ dataTransferSummary?.stablecoin?.valueInDollar }}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Market Cap</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									numberFormatter(
										Number(dataTransferSummary?.stablecoin?.totalSupply)
									)
								}}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Current Supply</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									numberFormatter(
										Number(dataTransferSummary?.stablecoin?.totalSupply)
									)
								}}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded"># of Holders</p>
							<p class="font-bold text-xl text-theme-interactive">
								{{ dataTransferSummary?.stablecoin?.totalUniqueHolders }}
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
							<p
								class="font-bold text-xl text-theme-interactive flex flex-row items-center"
							>
								<Tooltip
									v-if="dataTransferSummary?.stablecoin?.issuer"
									:text="dataTransferSummary?.stablecoin?.issuer"
									text-class="text-theme-body"
								>
									<UserIcon
										class="h-4 text-theme-white inline align-text-top"
									/>
									{{ shortenHash(dataTransferSummary?.stablecoin?.issuer) }}
								</Tooltip>
								<TextCopy
									v-if="dataTransferSummary?.stablecoin?.issuer"
									:text="dataTransferSummary?.stablecoin?.issuer"
									label="Click to copy block hash to clipboard"
									class="h-5 inline align-baseline"
									tooltip-class="font-sans"
								/>
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
		<Transactions
			v-else-if="selectedTab === 'transactions'"
			:coin-id="coinId"
		/>
		<Analytics v-else :coin-id="coinId" />
	</div>
</template>
<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { numberFormatter, shortenHash } from '~/utils/format'
import TabBar from '~/components/atoms/TabBar.vue'
import TabBarItem from '~/components/atoms/TabBarItem.vue'
import { useStableCoinTokenTransferQuery } from '~/queries/useStableCoinTokenTransferQuery'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import Holders from '~/components/StableCoin/Holders.vue'
import Transactions from '~/components/StableCoin/Transactions.vue'
import Analytics from '~/components/StableCoin/Analytics.vue'
import { useRoute } from 'vue-router'
const route = useRoute()

definePageMeta({
	middleware: 'plt-features-guard',
})

const coinId = computed(() => {
	const id = route.params.stablecoin
	return Array.isArray(id) ? id[0] : id || ''
})

const days = ref(30)

watch(
	() => coinId.value,
	(newId, oldId) => {
		console.log(`User ID changed from ${oldId} to ${newId}`)
	}
)

const { data: dataTransferSummary } = useStableCoinTokenTransferQuery(
	coinId.value.toUpperCase(),
	days
)

const selectedTab = ref('transactions')
const handleSelectTab = (tabId: string) => (selectedTab.value = tabId)
</script>
