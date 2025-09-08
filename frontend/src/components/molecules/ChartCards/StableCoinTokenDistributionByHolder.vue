<template>
	<div>
		<DistributionByHolder
			:distribution-values="distributionValues"
			:label-clickable="true"
		>
			<template #title> Token Distribution By Holder </template>
		</DistributionByHolder>
	</div>
</template>

<script lang="ts" setup>
import { ref, watchEffect, defineProps } from 'vue'
import DistributionByHolder from '~/components/molecules/DistributionByHolder.vue'
import { calculatePercentageforBigInt, shortenHash } from '~/utils/format'
import type { PltAccountAmount } from '~/types/generated'

type Props = {
	tokenDistributionData: PltAccountAmount[]
	totalSupply: bigint
}

const props = defineProps<Props>()

const distributionValues = ref<
	{ address: string; percentage: string; symbol: string }[]
>([])

// Transforms tokenTransferData into chart-ready format according to props data.
// Calculates percentage ownership and shortens addresses for display.
watchEffect(() => {
	distributionValues.value =
		props.tokenDistributionData.map(item => {
			const address = item.accountAddress.asString
			const amount = BigInt(item.amount.value)
			const supply = props.totalSupply
			const percentage = calculatePercentageforBigInt(amount, supply)
			const symbol = item.tokenId
			return {
				address: shortenHash(address),
				percentage: `${percentage}`,
				symbol,
			}
		}) ?? []
})
</script>
