<template>
	<div>
		<DistributionChartBar
			:stable-coins-data="stableCoinsData"
			chart-type="uniqueHolders"
			show-sign=""
		>
			<template #title> Holder by Stablecoin </template>
		</DistributionChartBar>
	</div>
</template>
<script lang="ts" setup>
import type { StablecoinResponse } from '~/queries/useStableCoinQuery'
import DistributionChartBar from '../DistributionChartBar.vue'

type Props = {
	stableCoinsData?: StablecoinResponse
	isLoading?: boolean
}

defineProps<Props>()
</script>
