<template>
	<div>
		<div v-if="pltEventMetricsLoading" class="w-full h-36 text-center">
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
						:is-loading="pltEventMetricsLoading"
						:transfer-summary="pltEventMetricsDataRef"
					/>
				</CarouselSlide>
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="topHolder"
						:data="[
							{ label: 'Top 10', value: TransactionFilterOption.Top10 },
							{ label: 'Top 20', value: TransactionFilterOption.Top20 },
						]"
					/>
					<StableCoinTokenDistributionByHolder
						:token-transfer-data="pagedData"
						:is-loading="pltHolderLoading"
						:total-supply="props.totalSupply"
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
import StableCoinTokenDistributionByHolder from '~/components/molecules/ChartCards/StableCoinTokenDistributionByHolder.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'
import { usePltEventsMetricsQueryByTokenId } from '~/queries/usePltEventsMetricsQuery'
import { MetricsPeriod, type PltaccountAmount } from '~/types/generated'
import { ref, watch } from 'vue'
import { TransactionFilterOption } from '~/types/stable-coin'
import { usePagedData } from '~/composables/usePagedData'
import { usePltTokenHolderQuery } from '~/queries/usePltTokenHolderQuery'

// Define Props
const props = defineProps<{
	coinId?: string
	totalSupply?: bigint
}>()
// Loading state

const coinId = props.coinId ?? ''
// Loading state

const topHolder = ref(TransactionFilterOption.Top10)
const days = ref(MetricsPeriod.Last7Days)
watch(days, newValue => {
	selectedMetricsPeriod.value = newValue
})

// Handle undefined props

const selectedMetricsPeriod = ref(MetricsPeriod.Last24Hours)

const { data: pltEventMetricsData, loading: pltEventMetricsLoading } =
	usePltEventsMetricsQueryByTokenId(selectedMetricsPeriod, coinId)
const pltEventMetricsDataRef = ref(pltEventMetricsData)
watch(
	pltEventMetricsData,
	newData => {
		pltEventMetricsDataRef.value = newData
	},
	{ immediate: true, deep: true }
)

const { pagedData, addPagedData } = usePagedData<PltaccountAmount>(
	[],
	topHolder.value,
	topHolder.value
)

const queryFirst = ref(topHolder.value)

watch(
	() => topHolder.value,
	newValue => {
		queryFirst.value = newValue
		pagedData.value = []
	}
)

const { data: pltHolderData, fetching: pltHolderLoading } =
	usePltTokenHolderQuery(coinId, {
		first: queryFirst,
	})

watch(
	() => pltHolderData.value,
	value => {
		if (value?.pltAccountsByTokenId) {
			addPagedData(
				value.pltAccountsByTokenId.nodes || [],
				value.pltAccountsByTokenId.pageInfo
			)
		}
	},
	{ immediate: true }
)
</script>
