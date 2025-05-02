<template>
	<div>
		<DistributionByHolder
			:is-loading="isLoading"
			:distribution-values="distributionValues"
			:label-clickable="false"
		>
			<template #title> Token Distribution By Holder </template>
		</DistributionByHolder>
	</div>
</template>

<script lang="ts" setup>
import { ref, watchEffect, defineProps } from 'vue'
import type { StableCoinDashboardListResponse } from '~/queries/useStableCoinDashboardList'
import DistributionByHolder from '~/components/molecules/DistributionByHolder.vue'
import { shortenHash } from '~/utils/format'

type Props = {
	tokenTransferData?: StableCoinDashboardListResponse
}
const props = defineProps<Props>()

const isLoading = ref(true)
const distributionValues = ref<
	{ address: string; percentage: string; symbol: string }[]
>([])

watchEffect(() => {
	const holders = props.tokenTransferData?.stablecoin?.holdings || []

	if (holders.length === 0) {
		distributionValues.value = []
		return
	}

	setTimeout(() => {
		distributionValues.value = holders.map(ele => ({
			address: shortenHash(ele.address),
			symbol: ele.assetName || '',
			percentage: (ele.percentage ?? 0).toFixed(2), // <-- formatted to 2 decimals
		}))
		isLoading.value = false
	}, 1000)
})
</script>
