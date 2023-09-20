<template>
	<span>
		Contract <Contract :address="event.contractAddress" /> upgraded module from
		<ModuleLink :module-reference="event.from" /> to module
		<ModuleLink :module-reference="event.to" />.
	</span>
</template>

<script setup lang="ts">
import ModuleLink from '../../molecules/ModuleLink.vue'
import { ContractUpgraded } from '~~/src/types/generated'
import Contract from '~/components/molecules/Contract.vue'

type Props = {
	event: ContractUpgraded
}
defineProps<Props>()
</script>
