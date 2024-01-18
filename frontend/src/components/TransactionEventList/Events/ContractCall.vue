<template>
	<span>
		<AddressLink :address="event.contractUpdated.instigator" />
		called {{ event.contractUpdated.receiveName }} on contract
		<ContractLink
			:address="event.contractUpdated.contractAddress.asString"
			:contract-address-index="event.contractUpdated.contractAddress.index"
			:contract-address-sub-index="
				event.contractUpdated.contractAddress.subIndex
			"
		/>
		with amount <Amount :amount="event.contractUpdated.amount" />.
	</span>
</template>

<script setup lang="ts">
import ContractLink from '../../molecules/ContractLink.vue'
import AddressLink from '../../molecules/AddressLink.vue'
import type { ContractCall } from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	event: ContractCall
}

defineProps<Props>()
</script>
