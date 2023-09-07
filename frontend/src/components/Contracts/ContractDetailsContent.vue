<template>
	<div>
		<ContractDetailsHeader :contract="contract" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-1 mb-4">
				<ContractDetailsAmounts :contract="contract" />
			</div>
			<Accordion :is-initial-open="true">
				Events
				<span class="numerical text-theme-faded"
					>({{ contract.contractEvents.length }})</span
				>
				<template #content>
					<ContractDetailsEvents :contract-events="contract.contractEvents" />
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Accordion from '../Accordion.vue'
import ContractDetailsAmounts from './ContractDetailsAmounts.vue'
import ContractDetailsHeader from './ContractDetailsHeader.vue'
import ContractDetailsEvents from './ContractDetailsEvents.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import { Contract } from '~~/src/types/generated'

type Props = {
	contract: Contract
}
defineProps<Props>()
</script>
