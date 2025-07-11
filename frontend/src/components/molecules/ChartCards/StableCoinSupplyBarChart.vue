<template>
	<div>
		<DistributionChartBar
			:stable-coins-data="stableCoinsData"
			chart-type="supply"
			show-sign=""
			:label-clickable="true"
		>
			<template #title> Protocol Token Supply </template>
		</DistributionChartBar>
	</div>
</template>
<script lang="ts" setup>
import type { Plttoken } from '~/types/generated'
import DistributionChartBar from '../DistributionChartBar.vue'

type Props = {
	stableCoinsData?: Plttoken[]
	isLoading?: boolean
}

defineProps<Props>()
</script>
