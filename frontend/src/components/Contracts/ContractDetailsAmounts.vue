<template>
	<DetailsCard>
		<template #title>Balance (Ͼ)</template>
		<template #default>
			<Amount :amount="contract.amount" data-testid="total-balance" />
		</template>
	</DetailsCard>
</template>

<script lang="ts" setup>
import DetailsCard from '~/components/DetailsCard.vue'
import Amount from '~/components/atoms/Amount.vue'
import type { Contract } from '~/types/generated'

type Props = { contract: Contract }

defineProps<Props>()
</script>
