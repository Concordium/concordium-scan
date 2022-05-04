<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="rewardMetricsData?.rewardMetricsForBaker?.buckets?.x_Time"
		:bucket-width="
			rewardMetricsData?.rewardMetricsForBaker?.buckets?.bucketWidth
		"
		:y-values="[
			rewardMetricsData?.rewardMetricsForBaker?.buckets?.y_SumRewards,
		]"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight></template>
		<template #title>Rewards</template>
		<template #icon>Ͼ</template>
		<template #value>{{
			convertMicroCcdToCcd(
				rewardMetricsData?.rewardMetricsForBaker?.sumRewardAmount
			)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import type { TooltipItem } from 'chart.js'
import { convertMicroCcdToCcd } from '~/utils/format'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import { type RewardMetricsForBakerQueryResponse } from '~/queries/useRewardMetricsForBakerQuery'
type Props = {
	rewardMetricsData: Ref<RewardMetricsForBakerQueryResponse | undefined>
	isLoading?: boolean
}
const formatLabel = (c: TooltipItem<'bar'>) => {
	return convertMicroCcdToCcd(c.parsed.y)
}
defineProps<Props>()
</script>
