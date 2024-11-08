<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="
			rewardMetricsData?.poolRewardMetricsForBakerPool?.buckets?.x_Time
		"
		:bucket-width="
			rewardMetricsData?.poolRewardMetricsForBakerPool?.buckets?.bucketWidth
		"
		:y-values="[yValues]"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight></template>
		<template #title>Rewards</template>
		<template #icon>Ͼ</template>
		<template #value>{{ chipValue }}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { TooltipItem } from 'chart.js'
import type { Ref } from 'vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import { type PoolRewardMetricsForBakerPoolResponse } from '~/queries/useBakerPoolRewardMetrics'
import { RewardTakerTypes } from '~/types/rewardTakerTypes'
type Props = {
	rewardMetricsData?: PoolRewardMetricsForBakerPoolResponse
	isLoading?: boolean
	rewardTakerType: RewardTakerTypes
}
const formatLabel = (c: TooltipItem<'bar'>) => {
	return convertMicroCcdToCcd(c.parsed.y)
}
const props = defineProps<Props>()
const yValues: Ref<number[] | undefined> = ref<number[]>()
const chipValue = ref()
const { rewardTakerType, rewardMetricsData } = toRefs(props)
watch([rewardTakerType, rewardMetricsData], () => {
	setType(rewardTakerType.value)
})
const setType = (newType: RewardTakerTypes) => {
	switch (newType) {
		case RewardTakerTypes.Baker:
			yValues.value =
				props.rewardMetricsData?.poolRewardMetricsForBakerPool.buckets.y_SumBakerRewards
			chipValue.value = convertMicroCcdToCcd(
				props.rewardMetricsData?.poolRewardMetricsForBakerPool
					?.sumBakerRewardAmount
			)
			break

		case RewardTakerTypes.Total:
			yValues.value =
				props.rewardMetricsData?.poolRewardMetricsForBakerPool.buckets.y_SumTotalRewards
			chipValue.value = convertMicroCcdToCcd(
				props.rewardMetricsData?.poolRewardMetricsForBakerPool
					?.sumTotalRewardAmount
			)
			break

		case RewardTakerTypes.Delegators:
			yValues.value =
				props.rewardMetricsData?.poolRewardMetricsForBakerPool.buckets.y_SumDelegatorsRewards
			chipValue.value = convertMicroCcdToCcd(
				props.rewardMetricsData?.poolRewardMetricsForBakerPool
					?.sumDelegatorsRewardAmount
			)
			break
	}
}
setType(rewardTakerType.value)
</script>
