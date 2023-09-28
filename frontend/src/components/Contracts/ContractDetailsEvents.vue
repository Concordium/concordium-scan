<template>
	<div>
		<Table :class="[$style.table, $style.contractDetail]">
			<TableHead>
				<TableRow>
					<TableTh>Transaction</TableTh>
					<TableTh>Age</TableTh>
					<TableTh>Type</TableTh>
					<TableTh>Details</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="(contractEvent, i) in contractEvents"
					:key="contractEvent"
				>
					<TableTd class="numerical">
						<TransactionLink :hash="contractEvent.transactionHash" />
					</TableTd>
					<TableTd>
						<Tooltip
							:text="
								convertTimestampToRelative(contractEvent.blockSlotTime, NOW)
							"
						>
							<DateTimeWithLineBreak :date-time="contractEvent.blockSlotTime" />
						</Tooltip>
					</TableTd>
					<TableTd>
						{{ trimTypeName(contractEvent.event.__typename) }}
					</TableTd>
					<TableTd>
						<template
							v-if="contractEvent.event.__typename === 'ContractInitialized'"
						>
							<DetailsView :id="i">
								<ContractInitialized :contract-event="contractEvent.event" />
							</DetailsView>
						</template>
						<template
							v-if="contractEvent.event.__typename === 'ContractUpdated'"
						>
							<DetailsView :id="i">
								<ContractUpdated :contract-event="contractEvent.event" />
							</DetailsView>
						</template>
						<template
							v-if="contractEvent.event.__typename === 'ContractModuleDeployed'"
						>
							<DetailsView :id="i">
								<div>Module Reference:</div>
								<div>
									<ModuleLink
										:module-reference="contractEvent.event.moduleRef"
									/>
								</div>
							</DetailsView>
						</template>
						<template v-if="contractEvent.event.__typename === 'ContractCall'">
							<DetailsView :id="i">
								<ContractCall :contract-event="contractEvent.event" />
							</DetailsView>
						</template>
						<template
							v-if="contractEvent.event.__typename === 'ContractUpgraded'"
						>
							<DetailsView :id="i">
								<div>
									<div>From Module</div>
									<div>
										<ModuleLink
											:module-reference="contractEvent.event.fromModule"
										/>
									</div>
								</div>
								<div>
									<div>To Module</div>
									<div>
										<ModuleLink
											:module-reference="contractEvent.event.toModule"
										/>
									</div>
								</div>
							</DetailsView>
						</template>
						<template
							v-if="contractEvent.event.__typename === 'ContractInterrupted'"
						>
							<DetailsView :id="i">
								<ContractInterrupted :contract-event="contractEvent.event" />
							</DetailsView>
						</template>
						<template
							v-if="contractEvent.event.__typename === 'ContractResumed'"
						>
							<DetailsView :id="i">
								<div>
									<div>Successfully Resumed:</div>
									<div>{{ contractEvent.event.success }}</div>
								</div>
							</DetailsView>
						</template>
						<template v-if="contractEvent.event.__typename === 'Transferred'">
							<DetailsView :id="i">
								<ContractTransfer :contract-event="contractEvent.event" />
							</DetailsView>
						</template>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from './DateTimeWithLineBreak.vue'
import DetailsView from './DetailsView.vue'
import ContractInitialized from './Events/ContractInitialized.vue'
import ContractCall from './Events/ContractCall.vue'
import ContractInterrupted from './Events/ContractInterrupted.vue'
import ContractTransfer from './Events/ContractTransfer.vue'
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import ContractUpdated from '~/components/Contracts/Events/ContractUpdated.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { ContractEvent, PageInfo } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import { convertTimestampToRelative } from '~~/src/utils/format'
import { PaginationTarget } from '~~/src/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	contractEvents: ContractEvent[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
const props = defineProps<Props>()

function trimTypeName(typeName: string | undefined) {
	let name = typeName
	if (typeName?.startsWith('Contract')) {
		name = typeName.slice(8)
	}
	return name
}
</script>
<style module>
.contractDetail table td {
	padding: 30px 20px 21px;
}
.table tr {
	border-bottom: 2px solid;
	border-bottom-color: var(--color-thead-bg);
}
.table tr:last-child {
	border-bottom: none;
}
</style>
