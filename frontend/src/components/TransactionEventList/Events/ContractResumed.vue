<template>
	<span v-if="event.success">
		Smart contract instance
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
		was successfully resumed
	</span>
	<span v-else>
		Smart contract instance
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
		was not resumed successfully
	</span>
</template>

<script setup lang="ts">
import ContractLink from '../../molecules/ContractLink.vue'
import type { ContractResumed } from '~/types/generated'

type Props = {
	event: ContractResumed
}

defineProps<Props>()
</script>
