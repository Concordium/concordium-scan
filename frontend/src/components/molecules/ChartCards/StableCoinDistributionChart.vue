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
import DistributionChart from '../DistributionChart.vue'
import type { Plttoken } from '~/types/generated'

type StableCoin = {
	totalSupply?: number
	symbol?: string
	supplyPercentage?: string
	totalUniqueHolder?: number
}

type Props = {
	stableCoinsData?: Plttoken[]
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

	setTimeout(() => {
		const totalSupplySum = stablecoins.reduce((sum, coin) => {
			const totalSupply = coin.totalSupply ?? 0
			const decimal = coin.decimal ?? 0
			return sum + totalSupply / 10 ** decimal
		}, 0)

		delayedSupplyPercentage.value = stablecoins.map(coin => ({
			symbol: coin.tokenId || '',
			supplyPercentage:
				totalSupplySum > 0
					? (
							((coin.totalSupply ?? 0) /
								10 ** (coin.decimal ?? 0) /
								totalSupplySum) *
							100
					  ).toString()
					: '0',
		}))
	}, 1000)
})
</script>
