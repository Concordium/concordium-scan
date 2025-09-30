<template>
	<div>
		<DistributionChart
			:is-loading="isLoading"
			:stable-coins-data="delayedSupplyPercentage"
			:label-clickable="true"
		>
			<template #title>Protocol Token Supply Distribution </template>
		</DistributionChart>
	</div>
</template>

<script lang="ts" setup>
import { ref, watchEffect, defineProps } from 'vue'
import DistributionChart from '../DistributionChart.vue'
import type { PltToken } from '~/types/generated'

type StableCoin = {
	totalSupply?: number
	symbol?: string
	supplyPercentage?: string
	totalUniqueHolder?: number
}

type Props = {
	stableCoinsData?: PltToken[]
	isLoading?: boolean
}

const props = defineProps<Props>()

const delayedSupplyPercentage = ref<StableCoin[]>([])

watchEffect(() => {
	const stablecoins = props.stableCoinsData || []

	if (stablecoins.length === 0) {
		delayedSupplyPercentage.value = []
		return
	}

	const totalSupplySum = stablecoins.reduce((sum, coin) => {
		return sum + (coin.normalizedCurrentSupply ?? 0)
	}, 0)

	delayedSupplyPercentage.value = stablecoins.map(coin => ({
		symbol: coin.tokenId || '',
		supplyPercentage:
			totalSupplySum > 0
				? (
						((coin.normalizedCurrentSupply ?? 0) / totalSupplySum) *
						100
				  ).toString()
				: '0',
	}))
})
</script>
