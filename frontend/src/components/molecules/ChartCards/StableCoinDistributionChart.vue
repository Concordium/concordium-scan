<template>
	<div>
		<DistributionChart
			:is-loading="isLoading"
			:stable-coins-data="delayedSupplyPercentage"
		>
			<template #title> Distribution Supply StableCoin </template>
		</DistributionChart>
	</div>
</template>

<script lang="ts" setup>
import { ref, watchEffect, defineProps } from 'vue'
import type { StablecoinResponse } from '~/queries/useStableCoinQuery'
import DistributionChart from '../DistributionChart.vue'

type StableCoin = {
	totalSupply?: number
	symbol?: string
	supplyPercentage?: string
	totalUniqueHolder?: number
}

type Props = {
	stableCoinsData?: StablecoinResponse
	isLoading?: boolean
}

const props = defineProps<Props>()

const delayedSupplyPercentage = ref<StableCoin[]>([])

watchEffect(() => {
	const stablecoins = props.stableCoinsData?.stablecoins || []

	if (stablecoins.length === 0) {
		delayedSupplyPercentage.value = []
		return
	}

	setTimeout(() => {
		const totalSupplySum = stablecoins.reduce(
			(sum, coin) => sum + (coin.totalSupply || 0),
			0
		)

		delayedSupplyPercentage.value = stablecoins.map(coin => ({
			symbol: coin.symbol,
			supplyPercentage:
				totalSupplySum > 0
					? ((coin.totalSupply! / totalSupplySum) * 100).toFixed(2)
					: '0',
		}))
	}, 1000)
})
</script>
