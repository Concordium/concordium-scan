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
					<template #title
						>Creator
						<InfoTooltip text="Account address of module creator" />
					</template>
					<template #default>
						<AccountLink :address="moduleReferenceEvent.sender.asString" />
					</template>
				</DetailsCard>
			</div>
			<div v-if="moduleReferenceEvent.displaySchema" class="schema-section" />
			<Tabs :tab-list="tabList">
				<template #tabPanel-1>
					<DetailsTable
						v-if="moduleReferenceEvent.linkedContracts?.items?.length"
						:total-count="moduleReferenceEvent.linkedContracts.totalCount"
						:page-offset-info="paginationLinkedContracts"
						:page-dropdown-info="pageDropdownLinkedContracts"
						:fetching="fetching"
					>
						<ModuleDetailsLinkedContracts
							:linked-contracts="moduleReferenceEvent.linkedContracts!.items"
						/>
					</DetailsTable>
				</template>
				<template #tabPanel-2>
					<DetailsTable
						v-if="
							moduleReferenceEvent.moduleReferenceContractLinkEvents?.items
								?.length
						"
						:total-count="
							moduleReferenceEvent.moduleReferenceContractLinkEvents.totalCount
						"
						:page-offset-info="paginationLinkingEvents"
						:page-dropdown-info="pageDropdownEvents"
						:fetching="fetching"
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
						:total-count="
							moduleReferenceEvent.moduleReferenceRejectEvents.totalCount
						"
						:page-offset-info="paginationRejectEvents"
						:page-dropdown-info="pageDropdownRejectedEvents"
						:fetching="fetching"
					>
						<ModuleDetailsRejectEvents
							:module-reject-events="moduleReferenceEvent.moduleReferenceRejectEvents!.items"
						/>
					</DetailsTable>
				</template>
				<template v-if="moduleReferenceEvent.displaySchema" #tabPanel-4>
					<div class="schema">
						<code>
							<pre>{{ moduleReferenceEvent.displaySchema }}</pre>
						</code>
					</div>
				</template>
			</Tabs>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Tabs from '../Tabs.vue'
import DetailsTable from '../Details/DetailsTable.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'
import ModuleDetailsHeader from './ModuleDetailsHeader.vue'
import ModuleDetailsContractLinkEvents from './ModuleDetailsContractLinkEvents.vue'
import ModuleDetailsLinkedContracts from './ModuleDetailsLinkedContracts.vue'
import ModuleDetailsRejectEvents from './ModuleDetailsRejectEvents.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import type { ModuleReferenceEvent } from '~/types/generated'
import { convertTimestampToRelative, formatTimestamp } from '~/utils/format'
import type { PaginationOffsetInfo } from '~/composables/usePaginationOffset'
import type { PageDropdownInfo } from '~/composables/usePageDropdown'

const { NOW } = useDateNow()

type Props = {
	moduleReferenceEvent: ModuleReferenceEvent
	paginationLinkingEvents: PaginationOffsetInfo
	paginationRejectEvents: PaginationOffsetInfo
	paginationLinkedContracts: PaginationOffsetInfo
	pageDropdownEvents: PageDropdownInfo
	pageDropdownRejectedEvents: PageDropdownInfo
	pageDropdownLinkedContracts: PageDropdownInfo
	fetching: boolean
}
const props = defineProps<Props>()
const tabList = computed(() => {
	const tabList = [
		`Linked contracts (${
			props.moduleReferenceEvent.linkedContracts?.totalCount ?? 0
		})`,
		`Linking events (${
			props.moduleReferenceEvent.moduleReferenceContractLinkEvents
				?.totalCount ?? 0
		})`,
		`Rejected events (${
			props.moduleReferenceEvent.moduleReferenceRejectEvents?.totalCount ?? 0
		})`,
	]
	if (props.moduleReferenceEvent.displaySchema) {
		tabList.push('Schema')
	}

	return tabList
})
</script>
<style>
.schema {
	padding: 5px;
	font-size: 0.9rem;

	@media screen and (max-width: 1024px) {
		font-size: 1rem;
	}
}
</style>
