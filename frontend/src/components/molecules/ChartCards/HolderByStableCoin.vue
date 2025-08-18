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
import DistributionChartBar from '../DistributionChartBar.vue'
import type { PltToken } from '~/types/generated'

type Props = {
	stableCoinsData?: PltToken[]
	isLoading?: boolean
}

defineProps<Props>()
</script>
