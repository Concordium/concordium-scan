<template>
	<div>
		<div v-if="isLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-2">
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinTokenTransfer :transfer-summary="dataTransferSummary" />
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<StableCoinTokenDistributionByHolder
						:token-transfer-data="dataPerStablecoin"
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

// Define Props
const props = defineProps<{
	coinId?: string
}>()

// Loading state
const isLoading = ref(true)

// Handle undefined props
const coinId = props.coinId?.toUpperCase() ?? 'USDC'

// Fetch Data
const { data: dataPerStablecoin } = useStableCoinDashboardList({
	symbol: coinId,
	topHolder: 12,
})

const { data: dataTransferSummary } = useStableCoinTokenTransferQuery(
	coinId,
	12
)

// Watch for data updates
watch(dataPerStablecoin, newData => {
	if (newData) {
		isLoading.value = false
	}
})
</script>
