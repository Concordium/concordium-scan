<template>
	<span>
		Smart contract instance
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
		was interupted
	</span>
</template>

<script setup lang="ts">
import ContractLink from '../../molecules/ContractLink.vue'
import type { ContractInterrupted } from '~/types/generated'

type Props = {
	event: ContractInterrupted
}

defineProps<Props>()
</script>
