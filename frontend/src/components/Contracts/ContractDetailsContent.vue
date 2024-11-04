<template>
	<div>
		<ContractDetailsHeader :contract-address="contract.contractAddress" />
		<DrawerContent>
			<div class="flex flex-row flex-wrap gap-5 md:gap-20 mb-6 md:mb-12">
				<DetailsCard>
					<template #title>Contract name</template>
					<template #default>
						{{ contract.snapshot.contractName }}
					</template>
				</DetailsCard>
				<ContractDetailsAmounts :contract="contract" />
				<DetailsCard>
					<template #title>Date</template>
					<template #default>
						{{ formatTimestamp(contract.blockSlotTime) }}
					</template>
					<template v-if="breakpoint >= Breakpoint.LG" #secondary>
						({{ convertTimestampToRelative(contract.blockSlotTime, NOW) }})
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>
						Module
						<InfoTooltip
							text="Container which holds execution code for one or more contracts. The below references hold the current execution code of the contract."
							position="bottom"
						/>
					</template>
					<template #default>
						<ModuleLink :module-reference="contract.snapshot.moduleReference" />
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>
						Creator
						<InfoTooltip
							text="Account address of the contract instance creator."
						/>
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
						:fetching="fetching"
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
						:fetching="fetching"
					>
						<ContractDetailsRejectEvents
							:contract-reject-events="contract.contractRejectEvents!.items"
						/>
					</DetailsTable>
				</template>
				<template
					v-if="
						contract.tokens?.items?.length && contract.tokens?.items?.length > 0
					"
					#tabPanel-3
				>
					<DetailsTable
						:total-count="contract.tokens.totalCount"
						:page-offset-info="paginationTokens"
						:page-dropdown-info="pageDropdownTokens"
						:fetching="fetching"
					>
						<ContractDetailsTokens :contract-tokens="contract.tokens!.items" />
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
import AccountLink from '../molecules/AccountLink.vue'
import ContractDetailsAmounts from './ContractDetailsAmounts.vue'
import ContractDetailsHeader from './ContractDetailsHeader.vue'
import ContractDetailsEvents from './ContractDetailsEvents.vue'
import ContractDetailsTokens from './ContractDetailsTokens.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import type { Contract } from '~~/src/types/generated'
import {
	convertTimestampToRelative,
	formatTimestamp,
} from '~~/src/utils/format'
import ContractDetailsRejectEvents from '~/components/Contracts/ContractDetailsRejectEvents.vue'
import type { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'
import type { PageDropdownInfo } from '~~/src/composables/usePageDropdown'
import { Breakpoint } from '~~/src/composables/useBreakpoint'

const { NOW } = useDateNow()

const { breakpoint } = useBreakpoint()

type Props = {
	contract: Contract
	paginationEvents: PaginationOffsetInfo
	paginationRejectEvents: PaginationOffsetInfo
	paginationTokens: PaginationOffsetInfo
	pageDropdownEvents: PageDropdownInfo
	pageDropdownRejectedEvents: PageDropdownInfo
	pageDropdownTokens: PageDropdownInfo
	fetching: boolean
}
const props = defineProps<Props>()

const tabList = computed(() => {
	const tabs = [
		`Event (${props.contract.contractEvents?.totalCount ?? 0})`,
		`Rejected events (${props.contract.contractRejectEvents?.totalCount ?? 0})`,
	]
	if (props.contract.tokens?.totalCount) {
		tabs.push(`Tokens (${props.contract.tokens.totalCount})`)
	}
	return tabs
})
</script>
