<template>
	<div>
		<DistributionChartBar
			:stable-coins-data="stableCoinsData"
			chart-type="uniqueHolders"
			show-sign=""
			:label-clickable="true"
		>
			<template #title> Protocol Token Holders </template>
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
