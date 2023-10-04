<template>
	<div>
		<ModuleDetailsHeader
			:module-reference="moduleReferenceEvent.moduleReference"
		/>
		<DrawerContent>
			<div class="flex flex-row gap-20 mb-12">
				<DetailsCard>
					<template #title>Age</template>
					<template #default>
						{{ formatTimestamp(moduleReferenceEvent.blockSlotTime) }}
					</template>
					<template #secondary>
						{{
							convertTimestampToRelative(
								moduleReferenceEvent.blockSlotTime,
								NOW
							)
						}}
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Creator</template>
					<template #default>
						<AccountLink :address="moduleReferenceEvent.sender.asString" />
					</template>
				</DetailsCard>
			</div>
			<Tabs :tab-list="tabList">
				<template #tabPanel-1>
					<DetailsTable
						v-if="moduleReferenceEvent.linkedContracts?.items?.length"
						:total-count="moduleReferenceEvent.linkedContracts.totalCount"
						:page-offset-info="paginationLinkedContracts"						
					>
						<ModuleDetailsLinkedContracts
							:linked-contracts="moduleReferenceEvent.linkedContracts!.items"
						/>
					</DetailsTable>
				</template>
				<template #tabPanel-2>
					<DetailsTable
						v-if="
							moduleReferenceEvent.moduleReferenceContractLinkEvents?.items?.length
						"
						:total-count="moduleReferenceEvent.moduleReferenceContractLinkEvents.totalCount"
						:page-offset-info="paginationLinkingEvents"
					>
						<ModuleDetailsContractLinkEvents
							:link-events="moduleReferenceEvent.moduleReferenceContractLinkEvents!.items"
						/>
					</DetailsTable>
				</template>
				<template #tabPanel-3>
					<DetailsTable
						v-if="
							moduleReferenceEvent.moduleReferenceRejectEvents?.items?.length
						"
						:total-count="moduleReferenceEvent.moduleReferenceRejectEvents.totalCount"
						:page-offset-info="paginationRejectEvents"
					>
						<ModuleDetailsRejectEvents
							:module-reject-events="moduleReferenceEvent.moduleReferenceRejectEvents!.items"
						/>
					</DetailsTable>
				</template>
			</Tabs>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Tabs from '../Tabs.vue'
import DetailsTable from '../Details/DetailsTable.vue'
import ModuleDetailsHeader from './ModuleDetailsHeader.vue'
import ModuleDetailsContractLinkEvents from './ModuleDetailsContractLinkEvents.vue'
import ModuleDetailsLinkedContracts from './ModuleDetailsLinkedContracts.vue'
import ModuleDetailsRejectEvents from './ModuleDetailsRejectEvents.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { ModuleReferenceEvent } from '~~/src/types/generated'
import { convertTimestampToRelative, formatTimestamp } from '~~/src/utils/format'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'

const { NOW } = useDateNow()

type Props = {
	moduleReferenceEvent: ModuleReferenceEvent
	paginationLinkingEvents: PaginationOffsetInfo
	paginationRejectEvents: PaginationOffsetInfo
	paginationLinkedContracts: PaginationOffsetInfo
}
const props = defineProps<Props>()
const tabList = computed(() => {
	return [
		`Linked Contracts (${
			props.moduleReferenceEvent.linkedContracts?.totalCount ?? 0
		})`,
		`Linking Events (${
			props.moduleReferenceEvent.moduleReferenceContractLinkEvents
				?.totalCount ?? 0
		})`,
		`Rejected Events (${
			props.moduleReferenceEvent.moduleReferenceRejectEvents?.totalCount ?? 0
		})`,
	]
})
</script>
