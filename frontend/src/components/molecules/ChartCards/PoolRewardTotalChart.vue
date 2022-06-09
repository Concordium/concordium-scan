<template>
	<KeyValueChartCard
		class="w-96 lg:w-full"
		:begin-at-zero="true"
		chart-type="bar"
		:x-values="
			rewardMetricsData?.poolRewardMetricsForPassiveDelegation?.buckets?.x_Time
		"
		:bucket-width="
			rewardMetricsData?.poolRewardMetricsForPassiveDelegation?.buckets
				?.bucketWidth
		"
		:y-values="[
			rewardMetricsData?.poolRewardMetricsForPassiveDelegation?.buckets
				?.y_SumTotalRewards,
		]"
		:is-loading="isLoading"
		:label-formatter="formatLabel"
	>
		<template #topRight></template>
		<template #title>Rewards</template>
		<template #icon>Ͼ</template>
		<template #value>{{
			convertMicroCcdToCcd(
				rewardMetricsData?.poolRewardMetricsForPassiveDelegation
					?.sumTotalRewardAmount
			)
		}}</template>
		<template #chip>sum</template>
	</KeyValueChartCard>
</template>
<script lang="ts" setup>
import type { TooltipItem } from 'chart.js'
import { convertMicroCcdToCcd } from '~/utils/format'
import KeyValueChartCard from '~/components/molecules/KeyValueChartCard.vue'
import { PoolRewardMetricsForPassiveDelegationResponse } from '~/queries/usePassiveDelegationPoolRewardMetrics'
type Props = {
	rewardMetricsData?: PoolRewardMetricsForPassiveDelegationResponse
	isLoading?: boolean
}
const formatLabel = (c: TooltipItem<'bar'>) => {
	return convertMicroCcdToCcd(c.parsed.y)
}
defineProps<Props>()
</script>
