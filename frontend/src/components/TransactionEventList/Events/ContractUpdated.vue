<template>
	<span>
		<ContractLink
			v-if="event.instigator.__typename === 'ContractAddress'"
			:address="event.instigator.asString"
			:contract-address-index="event.instigator.index"
			:contract-address-sub-index="event.instigator.subIndex"
		/>
		<AccountLink
			v-else-if="event.instigator.__typename === 'AccountAddress'"
			:address="event.instigator.asString"
		/>
		updated smart contract instance
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
	</span>
</template>

<script setup lang="ts">
import AccountLink from '../../molecules/AccountLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'
import type { ContractUpdated } from '~/types/generated'

type Props = {
	event: ContractUpdated
}

defineProps<Props>()
</script>
