<template>
	<div>
		<DistributionByHolder
			:is-loading="isLoading"
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
	tokenTransferData?: PltAccountAmount[]
	totalSupply?: bigint
}

const props = defineProps<Props>()

const isLoading = ref(true)
const distributionValues = ref<
	{ address: string; percentage: string; symbol: string }[]
>([])

watchEffect(() => {
	distributionValues.value =
		props.tokenTransferData?.map(item => {
			const address = item.accountAddress.asString
			const amount = BigInt(item.amount.value)
			const supply = props.totalSupply ?? BigInt(0)
			const percentage = calculatePercentageforBigInt(amount, supply)
			const symbol = item.tokenId ?? ''
			return {
				address: shortenHash(address),
				percentage: `${percentage}`,
				symbol,
			}
		}) ?? []
})
</script>
