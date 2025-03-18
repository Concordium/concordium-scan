<template>
	<div>
		<DistributionChartBar
			:is-loading="isLoading"
			:stable-coins-data="stableCoinsData"
		>
			<template #title> StableCoin Supply </template>
		</DistributionChartBar>
	</div>
</template>
<script lang="ts" setup>
// import type { StableCoinsQueryResponse } from '~/queries/useStableCoinQuery';
import DistributionChartBar from '../DistributionChartBar.vue'
type Props = {
	stableCoinsData: Array<unknown> | undefined
	isLoading?: boolean
}

defineProps<Props>()
</script>
