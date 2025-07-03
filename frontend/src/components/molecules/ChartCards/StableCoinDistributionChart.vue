<template>
	<div>
		<DistributionChart
			:is-loading="isLoading"
			:stable-coins-data="delayedSupplyPercentage"
			:label-clickable="false"
		>
			<template #title>Protocol Token Supply Distribution </template>
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
	const stablecoins = props.stableCoinsData?.liveStablecoins
		? [...props.stableCoinsData.liveStablecoins].sort(
				(a, b) => (b.totalSupply ?? 0) - (a.totalSupply ?? 0)
		  )
		: []

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
					? (((coin.totalSupply ?? 0) / totalSupplySum) * 100).toFixed(2)
					: '0',
		}))
	}, 1000)
})
</script>
