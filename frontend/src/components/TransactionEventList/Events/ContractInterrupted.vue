<template>
	<span>
		Smart contract instance
		<Contract :address="event.contractAddress" /> was interupted
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import type { ContractInterrupted } from '~/types/generated'

type Props = {
	event: ContractInterrupted
}

defineProps<Props>()
</script>
