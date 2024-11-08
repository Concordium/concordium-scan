<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="rewardMetricsData?.rewardMetricsForAccount?.buckets?.x_Time"
		:bucket-width="
			rewardMetricsData?.rewardMetricsForAccount?.buckets?.bucketWidth
		"
		:y-values="[
			rewardMetricsData?.rewardMetricsForAccount?.buckets?.y_SumRewards,
		]"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight></template>
		<template #title>Rewards</template>
		<template #icon>Ͼ</template>
		<template #value>{{
			convertMicroCcdToCcd(
				rewardMetricsData?.rewardMetricsForAccount?.sumRewardAmount
			)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { TooltipItem } from 'chart.js'
import { convertMicroCcdToCcd } from '~/utils/format'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import type { AccountRewardMetricsQueryResponse } from '~/queries/useAccountRewardMetricsQuery'
type Props = {
	rewardMetricsData?: AccountRewardMetricsQueryResponse
	isLoading?: boolean
}
const formatLabel = (c: TooltipItem<'bar'>) => {
	return convertMicroCcdToCcd(c.parsed.y)
}
defineProps<Props>()
</script>
