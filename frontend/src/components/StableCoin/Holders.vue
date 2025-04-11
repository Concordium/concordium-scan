<template>
	<div>
		<div v-if="isLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-1">
				<CarouselSlide class="w-full lg:h-full">
					<Table>
						<TableHead>
							<TableRow>
								<TableTh width="25%">Account</TableTh>
								<TableTh width="25%">Quantity</TableTh>
								<TableTh width="25%">Percentage</TableTh>
								<TableTh width="25%">Value</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow
								v-for="(coin, index) in dataPerStablecoin?.stablecoin?.holding"
								:key="index"
							>
								<TableTd>
									<Tooltip
										v-if="coin.address"
										:text="coin.address"
										text-class="text-theme-body"
									>
										{{ shortenHash(coin.address) }}
									</Tooltip>
									<TextCopy
										v-if="coin.address"
										:text="coin.address"
										label="Click to copy block hash to clipboard"
										class="h-5 inline align-baseline"
										tooltip-class="font-sans"
									/>
								</TableTd>
								<TableTd v-if="coin.holdings && coin.holdings.length > 0">
									{{ coin.holdings[0].quantity }}
								</TableTd>
								<TableTd v-if="coin.holdings && coin.holdings.length > 0">
									{{ coin.holdings[0].percentage?.toFixed(2) }}%
								</TableTd>
								<TableTd v-if="coin.holdings && coin.holdings.length > 0">
									<Tooltip
										:text="
											String(
												(coin.holdings[0].quantity ?? 0) *
													(dataPerStablecoin?.stablecoin?.valueInDoller ?? 0)
											)
										"
										text-class="text-theme-body"
									>
										${{ getCoinValue(coin.holdings[0]?.quantity) }}
									</Tooltip>
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { shortenHash } from '~/utils/format'
import { useStableCoinDashboardList } from '~/queries/useStableCoinDashboardList'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
// Define Props
const props = defineProps<{
	coinId?: string
}>()

// Loading state
const isLoading = ref(true)

const coinId = props.coinId?.toUpperCase() ?? 'USDC'

// Fetch Data
const { data: dataPerStablecoin } = useStableCoinDashboardList({
	symbol: coinId,
	topHolder: 12,
})

const getCoinValue = (quantity?: number) => {
	if (!quantity) return '0.00'
	const price = dataPerStablecoin?.value?.stablecoin?.valueInDoller ?? 0
	return (quantity * price).toFixed(2)
}

// Watch for data updates
watch(dataPerStablecoin, newData => {
	if (newData) {
		isLoading.value = false
	}
})
</script>
