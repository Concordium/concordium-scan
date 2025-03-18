<template>
	<div>
		<DistributionChart
			:is-loading="isLoading"
			:stable-coins-data="stableCoinsData"
		>
			<template #title> Distribution Supply StableCoin </template>
		</DistributionChart>
	</div>
</template>
<script lang="ts" setup>
// import type { StableCoinsQueryResponse } from '~/queries/useStableCoinQuery';
import DistributionChart from '../DistributionChart.vue'
type Props = {
	stableCoinsData: Array<unknown> | undefined
	isLoading?: boolean
}

defineProps<Props>()
</script>
