<template>
	<span>
		<AddressLink :address="event.instigator" />
		updated smart contract instance
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
	</span>
</template>

<script setup lang="ts">
import AddressLink from '../../molecules/AddressLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'
import type { ContractUpdated } from '~/types/generated'

type Props = {
	event: ContractUpdated
}

defineProps<Props>()
</script>
