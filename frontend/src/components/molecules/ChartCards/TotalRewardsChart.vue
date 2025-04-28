<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="rewardMetricsData?.rewardMetrics?.buckets?.x_Time"
		:bucket-width="rewardMetricsData?.rewardMetrics?.buckets?.bucketWidth"
		:y-values="[rewardMetricsData?.rewardMetrics?.buckets?.y_SumRewards]"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight />
		<template #title>Rewards</template>
		<template #icon>Ͼ</template>
		<template #value>{{
			convertMicroCcdToCcd(
				rewardMetricsData?.rewardMetrics?.sumRewardAmount
					? rewardMetricsData?.rewardMetrics?.sumRewardAmount
					: 0,
				true
			)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { TooltipItem } from 'chart.js'
import { convertMicroCcdToCcd } from '~/utils/format'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import type { RewardMetricsQueryResponse } from '~/queries/useRewardMetricsQuery'
type Props = {
	rewardMetricsData: RewardMetricsQueryResponse | undefined
	isLoading?: boolean
}
const formatLabel = (c: TooltipItem<'bar'>) => {
	return convertMicroCcdToCcd(c.parsed.y)
}
defineProps<Props>()
</script>
