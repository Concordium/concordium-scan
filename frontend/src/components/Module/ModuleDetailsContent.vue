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
					<template #title>Creator					
						<InfoTooltip text="Account address of module creator"/>
					</template>
					<template #default>
						<AccountLink :address="moduleReferenceEvent.sender.asString" />
					</template>
				</DetailsCard>
			</div>
			<Tabs :tab-list="tabList">
				<template #tabPanel-1>
					<DetailsTable
						v-if="moduleReferenceEvent.linkedContracts?.nodes?.length"
						:page-info="moduleReferenceEvent.linkedContracts!.pageInfo"
						:go-to-page="goToPageLinkedContract"
					>
						<ModuleDetailsLinkedContracts
							:linked-contracts="moduleReferenceEvent.linkedContracts!.nodes"
						/>
					</DetailsTable>
				</template>
				<template #tabPanel-2>
					<DetailsTable
						v-if="
							moduleReferenceEvent.moduleReferenceContractLinkEvents?.nodes
								?.length
						"
						:page-info="moduleReferenceEvent.moduleReferenceContractLinkEvents!.pageInfo"
						:go-to-page="goToPageEvents"
					>
						<ModuleDetailsContractLinkEvents
							:link-events="moduleReferenceEvent.moduleReferenceContractLinkEvents!.nodes"
						/>
					</DetailsTable>
				</template>
				<template #tabPanel-3>
					<DetailsTable
						v-if="
							moduleReferenceEvent.moduleReferenceRejectEvents?.nodes?.length
						"
						:page-info="moduleReferenceEvent.moduleReferenceRejectEvents!.pageInfo"
						:go-to-page="goToPageRejectEvents"
					>
						<ModuleDetailsRejectEvents
							:module-reject-events="moduleReferenceEvent.moduleReferenceRejectEvents!.nodes"
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
import InfoTooltip from '../atoms/InfoTooltip.vue'
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
