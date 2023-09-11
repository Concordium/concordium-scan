<template>
	<div>
		<ContractDetailsHeader :contract="contract" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<ContractDetailsAmounts :contract="contract" />
				<DetailsCard>
					<template #title>Age</template>
					<template #default>
						{{ convertTimestampToRelative(contract.blockSlotTime, NOW) }}
					</template>
					<template #secondary>
						{{ formatTimestamp(contract.blockSlotTime) }}
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Module</template>
					<template #default>
						<Hash :hash="contract.moduleReference" />
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Creator</template>
					<template #default>
						<AccountLink :address="contract.creator.asString" />
					</template>
				</DetailsCard>
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
			<Accordion :is-initial-open="true">
				Rejected Events
				<span class="numerical text-theme-faded"
					>({{ contract.contractRejectEvents.length }})</span
				>
				<template #content>
					<ContractDetailsRejectEvents
						:contract-reject-events="contract.contractRejectEvents"
					/>
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
import DetailsCard from '~/components/DetailsCard.vue'
import { Contract } from '~~/src/types/generated'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import ContractDetailsRejectEvents from '~/components/Contracts/ContractDetailsRejectEvents.vue'

const { NOW } = useDateNow()

type Props = {
	contract: Contract
}
defineProps<Props>()
</script>
