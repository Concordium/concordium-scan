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
						<ModuleLink :module-reference="contract.moduleReference" />
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Creator</template>
					<template #default>
						<AccountLink :address="contract.creator.asString" />
					</template>
				</DetailsCard>
			</div>
			<Tabs :tab-list="tabList">
				<template #tabPanel-1>
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
				<template #tabPanel-2>
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
			</Tabs>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Tabs from '../Tabs.vue'
import ModuleLink from '../molecules/ModuleLink.vue'
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
const props = defineProps<Props>()

const tabList = computed(() => {
	return [
		`Event (${props.contract.contractEvents?.totalCount ?? 0})`,
		`Rejected Events (${props.contract.contractRejectEvents?.totalCount ?? 0})`,
	]
})
</script>
