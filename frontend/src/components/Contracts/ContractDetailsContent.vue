<template>
	<div>
		<ContractDetailsHeader :contract-address="contract.contractAddress" />
		<DrawerContent>
			<div class="flex flex-row gap-20 mb-12">
				<DetailsCard>
					<template #title>Contract name</template>
					<template #default>
						{{ contract.contractName }}
					</template>
				</DetailsCard>				
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
					<template #title>
						Module
						<InfoTooltip text="Container which holds execution code for one or more contracts. The below references hold the current execution code of the contract."/>
					</template>
					<template #default>
						<ModuleLink :module-reference="contract.moduleReference" />
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>
						Creator
						<InfoTooltip text="Account address of the contract instance creator."/>
					</template>
					<template #default>
						<AccountLink :address="contract.creator.asString" />
					</template>
				</DetailsCard>
			</div>
			<Tabs :tab-list="tabList">
				<template #tabPanel-1>
					<DetailsTable
						v-if="
							contract.contractEvents?.items?.length &&
							contract.contractEvents?.items?.length > 0
						"
						:total-count="contract.contractEvents.totalCount"
						:page-offset-info="paginationEvents"
						:page-dropdown-info="pageDropdownEvents"
					>
						<ContractDetailsEvents
							:contract-events="contract.contractEvents!.items"
						/>
					</DetailsTable>
				</template>
				<template #tabPanel-2>
					<DetailsTable
						v-if="
							contract.contractRejectEvents?.items?.length &&
							contract.contractRejectEvents?.items?.length > 0
						"
						:total-count="contract.contractRejectEvents.totalCount"
						:page-offset-info="paginationRejectEvents"
						:page-dropdown-info="pageDropdownRejectedEvents"
					>
						<ContractDetailsRejectEvents
							:contract-reject-events="contract.contractRejectEvents!.items"
						/>
					</DetailsTable>
				</template>
			</Tabs>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import Tabs from '../Tabs.vue'
import ModuleLink from '../molecules/ModuleLink.vue'
import DetailsTable from '../Details/DetailsTable.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'
import ContractDetailsAmounts from './ContractDetailsAmounts.vue'
import ContractDetailsHeader from './ContractDetailsHeader.vue'
import ContractDetailsEvents from './ContractDetailsEvents.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { Contract } from '~~/src/types/generated'
import { convertTimestampToRelative, formatTimestamp } from '~~/src/utils/format'
import ContractDetailsRejectEvents from '~/components/Contracts/ContractDetailsRejectEvents.vue'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'
import { PageDropdownInfo } from '~~/src/composables/usePageDropdown'


const { NOW } = useDateNow()

type Props = {
	contract: Contract
	paginationEvents: PaginationOffsetInfo
	paginationRejectEvents: PaginationOffsetInfo
	pageDropdownEvents: PageDropdownInfo
	pageDropdownRejectedEvents: PageDropdownInfo
}
const props = defineProps<Props>()

const tabList = computed(() => {
	return [
		`Event (${props.contract.contractEvents?.totalCount ?? 0})`,
		`Rejected events (${props.contract.contractRejectEvents?.totalCount ?? 0})`,
	]
})
</script>
