<template>
	<span>
		<Contract
			v-if="event.contractUpdated.instigator.__typename === 'ContractAddress'"
			:address="event.contractUpdated.instigator"
		/>
		called {{ event.contractUpdated.receiveName }} on contract
		<Contract :address="event.contractUpdated.contractAddress" /> with amount
		<Amount :amount="event.contractUpdated.amount" />.
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import type { ContractCall } from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	event: ContractCall
}

defineProps<Props>()
</script>
