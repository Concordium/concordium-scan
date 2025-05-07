<template>
	<div>
		<div v-if="isLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="days"
						:data="[
							{ label: '7 Days', value: 7 },
							{ label: '1 month', value: 30 },
							{ label: '3 months', value: 90 },
						]"
					/>
					<StableCoinTokenTransfer
						:is-loading="transferLoading"
						:transfer-summary="dataTransferSummary"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="topHolder"
						:data="[
							{ label: 'Top 10', value: 10 },
							{ label: 'Top 20', value: 20 },
						]"
					/>
					<StableCoinTokenDistributionByHolder
						:token-transfer-data="dataPerStablecoin"
						:is-loading="holderLoading"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { useStableCoinDashboardList } from '~/queries/useStableCoinDashboardList'
import { useStableCoinTokenTransferQuery } from '~/queries/useStableCoinTokenTransferQuery'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinTokenTransfer from '~/components/molecules/ChartCards/StableCoinTokenTransfer.vue'
import StableCoinTokenDistributionByHolder from '~/components/molecules/ChartCards/StableCoinTokenDistributionByHolder.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'

// Define Props
const props = defineProps<{
	coinId?: string
}>()

// Loading state
const isLoading = ref(true)
const topHolder = ref(10)
const lastNTransactions = ref(20)
const days = ref(7)

// Handle undefined props
const coinId = props.coinId?.toUpperCase() ?? 'USDC'

const { data: dataPerStablecoin, fetching: holderLoading } =
	useStableCoinDashboardList(coinId, topHolder, lastNTransactions)

const { data: dataTransferSummary, fetching: transferLoading } =
	useStableCoinTokenTransferQuery(coinId, days)

// Watch for data updates
watch(dataPerStablecoin, newData => {
	if (newData) {
		isLoading.value = false
	}
})
</script>
