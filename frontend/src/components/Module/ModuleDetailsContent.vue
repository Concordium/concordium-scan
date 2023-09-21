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
			<Tabs :tab-list="tabList">
				<template #tabPanel-1>
					<ModuleDetailsLinkedContracts
						v-if="moduleReferenceEvent.linkedContracts?.nodes?.length"
						:linked-contracts="moduleReferenceEvent.linkedContracts!.nodes"
						:page-info="moduleReferenceEvent.linkedContracts!.pageInfo"
						:go-to-page="goToPageLinkedContract"
					/>
				</template>
				<template #tabPanel-2>
					<ModuleDetailsContractLinkEvents
						v-if="
							moduleReferenceEvent.moduleReferenceContractLinkEvents?.nodes
								?.length
						"
						:link-events="moduleReferenceEvent.moduleReferenceContractLinkEvents!.nodes"
						:page-info="moduleReferenceEvent.moduleReferenceContractLinkEvents!.pageInfo"
						:go-to-page="goToPageEvents"
					/>
				</template>
				<template #tabPanel-3>
					<ModuleDetailsRejectEvents
						v-if="
							moduleReferenceEvent.moduleReferenceRejectEvents?.nodes?.length
						"
						:module-reject-events="moduleReferenceEvent.moduleReferenceRejectEvents!.nodes"
						:page-info="moduleReferenceEvent.moduleReferenceRejectEvents!.pageInfo"
						:go-to-page="goToPageRejectEvents"
					/>
				</template>
			</Tabs>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Tabs from '../Tabs.vue'
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
