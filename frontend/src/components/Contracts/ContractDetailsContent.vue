<template>
	<div>
		<ContractDetailsHeader :contract-address="contract.contractAddress" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-4 mb-16">
				<ContractDetailsAmounts :contract="contract" />
				<DetailsCard>
					<template #title>Date</template>
					<template #default>
						{{ formatTimestamp(contract.blockSlotTime) }}
					</template>
					<template #secondary>
						({{ convertTimestampToRelative(contract.blockSlotTime, NOW) }})
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
					>({{ contract.contractEvents?.totalCount }})</span
				>
				<template #content>
					<ContractDetailsEvents
						v-if="
							contract.contractEvents?.nodes?.length &&
							contract.contractEvents?.nodes?.length > 0
						"
						:contract-events="contract.contractEvents!.nodes"
						:page-info="contract.contractEvents!.pageInfo"
						:go-to-page="goToPageEvents"
					/>
				</template>
			</Accordion>
			<Accordion :is-initial-open="true">
				Rejected Events
				<span class="numerical text-theme-faded"
					>({{ contract.contractRejectEvents?.totalCount }})</span
				>
				<template #content>
					<ContractDetailsRejectEvents
						v-if="
							contract.contractRejectEvents?.nodes?.length &&
							contract.contractRejectEvents?.nodes?.length > 0
						"
						:contract-reject-events="contract.contractRejectEvents!.nodes"
						:page-info="contract.contractRejectEvents!.pageInfo"
						:go-to-page="goToPageRejectEvents"
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
import { Contract, PageInfo } from '~~/src/types/generated'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import ContractDetailsRejectEvents from '~/components/Contracts/ContractDetailsRejectEvents.vue'
import type { PaginationTarget } from '~/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	contract: Contract
	goToPageEvents: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageRejectEvents: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
