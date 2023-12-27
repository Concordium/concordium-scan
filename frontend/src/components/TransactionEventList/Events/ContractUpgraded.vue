<template>
	<span>
		Contract
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
		upgraded module from <ModuleLink :module-reference="event.fromModule" /> to
		module <ModuleLink :module-reference="event.toModule" />.
	</span>
</template>

<script setup lang="ts">
import ModuleLink from '../../molecules/ModuleLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'
import { ContractUpgraded } from '~~/src/types/generated'

type Props = {
	event: ContractUpgraded
}
defineProps<Props>()
</script>
