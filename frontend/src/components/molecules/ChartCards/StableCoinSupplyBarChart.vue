<template>
	<div>
		<DistributionChartBar
			:stable-coins-data="stableCoinsData"
			chart-type="supply"
			show-sign="$"
		>
			<template #title> StableCoin Supply </template>
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
