<template>
	<div>
		<ModuleDetailsHeader
			:module-reference="moduleReferenceEvent.moduleReference"
		/>
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Age</template>
					<template #default>
						{{
							convertTimestampToRelative(
								moduleReferenceEvent.blockSlotTime,
								NOW
							)
						}}
					</template>
					<template #secondary>
						{{ formatTimestamp(moduleReferenceEvent.blockSlotTime) }}
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Creator</template>
					<template #default>
						<AccountLink :address="moduleReferenceEvent.sender.asString" />
					</template>
				</DetailsCard>
			</div>
			<Accordion :is-initial-open="true">
				Linked Contracts
				<span class="numerical text-theme-faded"
					>({{ moduleReferenceEvent.linkedContracts?.totalCount }})</span
				>
				<template #content>
					<ModuleDetailsLinkedContracts
						v-if="
							moduleReferenceEvent.linkedContracts?.nodes?.length &&
							moduleReferenceEvent.linkedContracts?.nodes?.length > 0
						"
						:linked-contracts="moduleReferenceEvent.linkedContracts!.nodes"
						:page-info="contract.contractEvents!.pageInfo"
						:go-to-page="goToPageLinkedContract"
					/>
				</template>
			</Accordion>
			<Accordion :is-initial-open="true">
				Linking Events
				<span class="numerical text-theme-faded"
					>({{
						moduleReferenceEvent.moduleReferenceContractLinkEvents?.totalCount
					}})</span
				>
				<template #content>
					<ModuleDetailsContractLinkEvents
						v-if="
							moduleReferenceEvent.moduleReferenceContractLinkEvents?.nodes
								?.length &&
							moduleReferenceEvent.moduleReferenceContractLinkEvents?.nodes
								?.length > 0
						"
						:link-events="moduleReferenceEvent.moduleReferenceContractLinkEvents!.nodes"
						:page-info="moduleReferenceEvent.moduleReferenceContractLinkEvents!.pageInfo"
						:go-to-page="goToPageEvents"
					/>
				</template>
			</Accordion>
			<Accordion :is-initial-open="true">
				Rejected Events
				<span class="numerical text-theme-faded"
					>({{
						moduleReferenceEvent.moduleReferenceRejectEvents?.totalCount
					}})</span
				>
				<template #content>
					<ModuleDetailsRejectEvents
						v-if="
							moduleReferenceEvent.moduleReferenceRejectEvents?.nodes?.length &&
							moduleReferenceEvent.moduleReferenceRejectEvents?.nodes?.length >
								0
						"
						:module-reject-events="moduleReferenceEvent.moduleReferenceRejectEvents!.nodes"
						:page-info="moduleReferenceEvent.moduleReferenceRejectEvents!.pageInfo"
						:go-to-page="goToPageRejectEvents"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Accordion from '../Accordion.vue'
import ModuleDetailsHeader from './ModuleDetailsHeader.vue'
import ModuleDetailsContractLinkEvents from './ModuleDetailsContractLinkEvents.vue'
import ModuleDetailsLinkedContracts from './ModuleDetailsLinkedContracts.vue'
import ModuleDetailsRejectEvents from './ModuleDetailsRejectEvents.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { ModuleReferenceEvent, PageInfo } from '~~/src/types/generated'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import type { PaginationTarget } from '~/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	moduleReferenceEvent: ModuleReferenceEvent
	goToPageEvents: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageRejectEvents: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageLinkedContract: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
