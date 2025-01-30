<template>
	<span>
		Contract
		<ContractLink
			:address="event.contractAddress.asString"
			:contract-address-index="event.contractAddress.index"
			:contract-address-sub-index="event.contractAddress.subIndex"
		/>
		<!-- @vue-expect-error Typescript is not aware that event.from got renamed to fromModule in the query -->
		upgraded module from <ModuleLink :module-reference="event.fromModule" /> to
		<!-- @vue-expect-error Typescript is not aware that event.to got renamed to toModule in the query -->
		module <ModuleLink :module-reference="event.toModule" />.
	</span>
</template>

<script setup lang="ts">
import ModuleLink from '../../molecules/ModuleLink.vue'
import ContractLink from '../../molecules/ContractLink.vue'
import type { ContractUpgraded } from '~~/src/types/generated'

type Props = {
	event: ContractUpgraded
}
defineProps<Props>()
</script>
