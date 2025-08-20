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
							{ label: '7 Days', value: MetricsPeriod.Last7Days },
							{ label: '1 month', value: MetricsPeriod.Last30Days },
							{ label: '3 months', value: MetricsPeriod.Last90Days },
						]"
					/>
					<StableCoinTokenTransfer
						:is-loading="isLoading"
						:transfer-summary="pltTransferMetricsDataRef"
						:decimals="
							pltTransferMetricsDataRef?.pltTransferMetricsByTokenId.decimal || 0
						"
					/>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import StableCoinTokenTransfer from '~/components/molecules/ChartCards/StableCoinTokenTransfer.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'
import { usePltTransferMetricsQueryByTokenId } from '~/queries/usePltTransferMetricsQuery'
import { MetricsPeriod } from '~/types/generated'
import { ref, watch } from 'vue'

const props = defineProps<{
	coinId: string
	totalSupply: bigint
}>()

const coinId = props.coinId

const days = ref(MetricsPeriod.Last7Days)
watch(days, newValue => {
	selectedMetricsPeriod.value = newValue
})

const selectedMetricsPeriod = ref(MetricsPeriod.Last24Hours)

const { data: pltTransferMetricsData, loading: isLoading } =
	usePltTransferMetricsQueryByTokenId(selectedMetricsPeriod, coinId)
const pltTransferMetricsDataRef = ref(pltTransferMetricsData)
watch(
	pltTransferMetricsData,
	newData => {
		pltTransferMetricsDataRef.value = newData
	},
	{ immediate: true, deep: true }
)
</script>
