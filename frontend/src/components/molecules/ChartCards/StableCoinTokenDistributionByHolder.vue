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
import { shortenHash } from '~/utils/format'
import type { PltaccountAmount } from '~/types/generated'

type Props = {
	tokenTransferData?: PltaccountAmount[]
	totalSupply?: bigint
}

const props = defineProps<Props>()

const isLoading = ref(true)
const distributionValues = ref<
	{ address: string; percentage: string; symbol: string }[]
>([])

watchEffect(() => {
	distributionValues.value = []
	props.tokenTransferData?.map(item => {
		const address = item.accountAddress.asString
		const amount = BigInt(item.amount.value)
		const percentage = (Number(amount) / Number(props.totalSupply ?? 0n)) * 100
		const symbol = item.tokenId ?? ''
		distributionValues.value.push({
			address: shortenHash(address),
			percentage: `${percentage}`,
			symbol,
		})
	})
})
</script>
