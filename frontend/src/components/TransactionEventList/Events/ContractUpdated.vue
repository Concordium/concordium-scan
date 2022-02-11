<template>
	<span>
		<Contract
			v-if="event.instigator.__typename === 'ContractAddress'"
			:address="event.instigator"
		/>
		<Hash
			v-else-if="event.instigator.__typename === 'AccountAddress'"
			:hash="event.instigator.address"
		/>
		updated smart contract instance
		<Contract :address="event.contractAddress" />
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import Hash from '~/components/molecules/Hash.vue'
import type { ContractUpdated } from '~/types/generated'

type Props = {
	event: ContractUpdated
}

defineProps<Props>()
</script>
