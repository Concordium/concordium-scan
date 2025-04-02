<template>
	<div>
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
</template>
<script lang="ts" setup>
import { useStableCoinDashboardList } from '~/queries/useStableCoinDashboardList'
import { useStableCoinTokenTransferQuery } from '~/queries/useStableCoinTokenTransferQuery'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinTokenTransfer from '~/components/molecules/ChartCards/StableCoinTokenTransfer.vue'
import StableCoinTokenDistributionByHolder from '~/components/molecules/ChartCards/StableCoinTokenDistributionByHolder.vue'

const { data: dataPerStablecoin } = useStableCoinDashboardList('USDC', 12)
const { data: dataTransferSummary } = useStableCoinTokenTransferQuery(
	'USDC',
	12
)
</script>
