<template>
	<div>
		<Title>CCDScan | Stable Coin</Title>
		<FtbCarousel non-carousel-classes="grid-cols-3">
			<CarouselSlide class="w-full lg:h-full">
				<div
					class="flex flex-col p-8 h-full w-full my-4 relative cardShadow rounded-2xl shadow-2xl overflow-hidden bg-theme-background-primary-elevated"
				>
					<h1>Profile</h1>
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
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0">Name</p>
							<p
								class="font-bold text-xl text-theme-interactive text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								{{ pltTokenDataRef?.name }}
							</p>
						</div>
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0">Symbol</p>
							<p
								class="font-bold text-xl text-theme-interactive text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								{{ pltTokenDataRef?.tokenId }}
							</p>
						</div>
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0">Decimals</p>
							<p
								class="font-bold text-xl text-theme-interactive text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								{{ pltTokenDataRef?.decimal }}
							</p>
						</div>
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0">Issuer</p>
							<p
								class="font-bold text-xl text-theme-interactive flex flex-row items-center text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								<AccountLink :address="pltTokenDataRef?.issuer.asString" />
							</p>
						</div>
					</div>
				</div>
			</CarouselSlide>
			<CarouselSlide class="w-full lg:h-full">
				<div
					class="flex flex-col p-8 h-full w-full my-4 relative cardShadow rounded-2xl shadow-2xl bg-theme-background-primary-elevated"
				>
					<h1>Configuration</h1>
					<div class="flex flex-col">
						<div class="flex justify-between pt-4">
							<div class="flex items-center gap-2">
								<p class="text-xl text-theme-faded">Mintable</p>
								<Tooltip
									text="Whether additional tokens can be minted"
									position="bottom"
									x="25"
									y="25"
									tooltip-position="fixed"
									class="text-theme-white text-right whitespace-nowrap z-[9999]"
								>
									<InfoIcon class="w-5 h-5 text-theme-faded cursor-pointer" />
								</Tooltip>
							</div>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									pltTokenDataRef?.tokenCreationDetails?.createPlt
										?.initializationParameters?.mintable
										? 'Yes'
										: 'No'
								}}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<div class="flex items-center gap-2">
								<p class="text-xl text-theme-faded">Burnable</p>
								<Tooltip
									text="Whether tokens can be burnt"
									position="bottom"
									tooltip-position="fixed"
									x="25"
									y="25"
									class="text-theme-white text-right whitespace-nowrap z-[9999]"
								>
									<InfoIcon class="w-5 h-5 text-theme-faded cursor-pointer" />
								</Tooltip>
							</div>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									pltTokenDataRef?.tokenCreationDetails?.createPlt
										?.initializationParameters?.burnable
										? 'Yes'
										: 'No'
								}}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<div class="flex items-center gap-2">
								<p class="text-xl text-theme-faded">Allow List</p>
								<Tooltip
									text="Whether token supports an allow list"
									position="bottom"
									x="25"
									y="25"
									tooltip-position="fixed"
									class="text-theme-white text-right whitespace-nowrap z-[9999]"
								>
									<InfoIcon class="w-5 h-5 text-theme-faded cursor-pointer" />
								</Tooltip>
							</div>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									pltTokenDataRef?.tokenCreationDetails?.createPlt
										?.initializationParameters?.allowList
										? 'Yes'
										: 'No'
								}}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<div class="flex items-center gap-2">
								<p class="text-xl text-theme-faded">Deny List</p>
								<Tooltip
									text="Whether token supports a deny list"
									position="bottom"
									x="25"
									y="25"
									tooltip-position="fixed"
									class="text-theme-white text-right whitespace-nowrap z-[9999]"
								>
									<InfoIcon class="w-5 h-5 text-theme-faded cursor-pointer" />
								</Tooltip>
							</div>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									pltTokenDataRef?.tokenCreationDetails?.createPlt
										?.initializationParameters?.denyList
										? 'Yes'
										: 'No'
								}}
							</p>
						</div>
						<div class="flex justify-between pt-4">
							<div class="flex items-center gap-2">
								<p class="text-xl text-theme-faded">Paused</p>
								<Tooltip
									text="This indicates whether the PLT is paused or unpaused. The pause operation sets the token into a global pause state in which no balance-changing operations can be carried out. "
									position="bottom"
									x="25"
									y="25"
									tooltip-position="fixed"
									class="text-theme-white text-right whitespace-nowrap z-[9999]"
								>
									<InfoIcon class="w-5 h-5 text-theme-faded cursor-pointer" />
								</Tooltip>
							</div>
							<p class="font-bold text-xl text-theme-interactive">
								{{
									pltTokenDataRef?.tokenModulePaused == 'true' ? 'Yes' : 'No'
								}}
							</p>
						</div>
					</div>
				</div>
			</CarouselSlide>
			<CarouselSlide class="w-full lg:h-full">
				<div
					class="flex flex-col p-8 h-full w-full my-4 relative cardShadow rounded-2xl shadow-2xl overflow-hidden bg-theme-background-primary-elevated"
				>
					<h1>Market</h1>
					<div class="flex flex-col">
						<!-- <div class="flex justify-between pt-4">
							<p class="text-xl text-theme-faded">Price</p>
							<p class="font-bold text-xl text-theme-interactive">
								${{ dataTransferSummary?.stablecoin?.valueInDollar }}
							</p>
						</div> -->
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0">
								Initial Supply
							</p>
							<p
								class="font-bold text-xl text-theme-interactive text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								<PltAmount
									:value="(pltTokenDataRef?.initialSupply ?? 0).toString()"
									:decimals="pltTokenDataRef?.decimal ?? 0"
									:format-number="true"
								/>
							</p>
						</div>
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0">
								Current Supply
							</p>
							<p
								class="font-bold text-xl text-theme-interactive text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								<PltAmount
									:value="(pltTokenDataRef?.totalSupply ?? 0).toString()"
									:decimals="pltTokenDataRef?.decimal ?? 0"
									:format-number="true"
								/>
							</p>
						</div>
						<div class="flex justify-between pt-4 gap-4">
							<p class="text-xl text-theme-faded flex-shrink-0"># of Holders</p>
							<p
								class="font-bold text-xl text-theme-interactive text-right break-words overflow-wrap-anywhere max-w-[60%]"
							>
								{{ pltTokenDataRef?.totalUniqueHolders }}
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
						Activities
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
		<Holders
			v-if="selectedTab === 'holders'"
			:coin-id="coinId"
			:total-supply="BigInt(pltTokenDataRef?.totalSupply ?? 0)"
		/>
		<Transactions
			v-else-if="selectedTab === 'transactions'"
			:coin-id="coinId"
		/>
		<Analytics
			v-else
			:coin-id="coinId"
			:total-supply="BigInt(pltTokenDataRef?.totalSupply ?? 0)"
		/>
	</div>
</template>
<script lang="ts" setup>
import { computed, ref, watch } from 'vue'
import TabBar from '~/components/atoms/TabBar.vue'
import TabBarItem from '~/components/atoms/TabBarItem.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import Holders from '~/components/StableCoin/Holders.vue'
import Transactions from '~/components/StableCoin/Transactions.vue'
import { usePltTokenQueryById } from '~/queries/usePltTokenQuery'

import { useRoute } from 'vue-router'
import Analytics from '~/components/StableCoin/Analytics.vue'

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
