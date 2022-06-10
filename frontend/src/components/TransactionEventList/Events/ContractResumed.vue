<template>
	<span v-if="event.success">
		Smart contract instance
		<Contract :address="event.contractAddress" /> was successfully resumed
	</span>
	<span v-else>
		Smart contract instance
		<Contract :address="event.contractAddress" /> was not resumed successfully
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import type { ContractResumed } from '~/types/generated'

type Props = {
	event: ContractResumed
}

defineProps<Props>()
</script>
