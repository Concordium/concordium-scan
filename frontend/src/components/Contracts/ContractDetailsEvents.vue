<template>
	<TableHead>
		<TableRow>
			<TableTh>Transaction</TableTh>
			<TableTh>Age</TableTh>
			<TableTh>Type</TableTh>
			<TableTh>Details</TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="(contractEvent, i) in contractEvents" :key="contractEvent">
			<TableTd class="numerical">
				<TransactionLink :hash="contractEvent.transactionHash" />
			</TableTd>
			<TableTd>
				<Tooltip
					:text="convertTimestampToRelative(contractEvent.blockSlotTime, NOW)"
				>
					<DateTimeWithLineBreak :date-time="contractEvent.blockSlotTime" />
				</Tooltip>
			</TableTd>
			<TableTd>
				{{ trimTypeName(contractEvent.event.__typename) }}
				<InfoTooltip :text="getEventTooltip(contractEvent.event.__typename!)"/>
			</TableTd>
			<TableTd>
				<DetailsView
					v-if="contractEvent.event.__typename === 'ContractInitialized'"
					:id="i"
				>
					<ContractInitialized :contract-event="contractEvent.event" />
				</DetailsView>
				<DetailsView
					v-if="contractEvent.event.__typename === 'ContractUpdated'"
					:id="i"
				>
					<ContractUpdated :contract-event="contractEvent.event" />
				</DetailsView>
				<DetailsView
					v-if="contractEvent.event.__typename === 'ContractCall'"
					:id="i"
				>
					<ContractCall :contract-event="contractEvent.event" />
				</DetailsView>
				<DetailsView
					v-if="contractEvent.event.__typename === 'ContractUpgraded'"
					:id="i"
				>
					<div>
						<div>From Module</div>
						<div>
							<ModuleLink :module-reference="contractEvent.event.fromModule" />
						</div>
					</div>
					<div>
						<div>To Module</div>
						<div>
							<ModuleLink :module-reference="contractEvent.event.toModule" />
						</div>
					</div>
				</DetailsView>
				<DetailsView
					v-if="contractEvent.event.__typename === 'ContractInterrupted'"
					:id="i"
				>
					<div>Expand to see logs</div>
					<LogsHEX :events-as-hex="contractEvent.event.eventsAsHex" />
				</DetailsView>
				<DetailsView
					v-if="contractEvent.event.__typename === 'ContractResumed'"
					:id="i"
				>
					<div> {{ getResumedLabel(contractEvent.event.success) }}</div>
				</DetailsView>
				<DetailsView
					v-if="contractEvent.event.__typename === 'Transferred'"
					:id="i"
				>
					<ContractTransfer :contract-event="contractEvent.event" />
				</DetailsView>
			</TableTd>
		</TableRow>
	</TableBody>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from '../Details/DateTimeWithLineBreak.vue'
import DetailsView from '../Details/DetailsView.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'
import LogsHEX from '../Details/LogsHEX.vue'
import ContractInitialized from './Events/ContractInitialized.vue'
import ContractCall from './Events/ContractCall.vue'
import ContractTransfer from './Events/ContractTransfer.vue'
import ModuleLink from '~/components/molecules/ModuleLink.vue'
import ContractUpdated from '~/components/Contracts/Events/ContractUpdated.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { ContractEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import { convertTimestampToRelative } from '~~/src/utils/format'

const { NOW } = useDateNow()

type Props = {
	contractEvents: ContractEvent[]
}
defineProps<Props>()

function getResumedLabel(resumed: boolean) : string {
	return resumed ? "Sucessfully resumed" : "Failed"
}

function getEventTooltip(eventType: string) {
	switch (eventType) {
        case 'ContractInitialized':
            return "TODO";
        case 'ContractUpdated':
            return "TODO";
        case 'ContractModuleDeployed':
            return "TODO";
        case 'ContractCall':
            return "TODO";
        case 'ContractUpgraded':
            return "TODO";
        case 'ContractResumed':
            return "TODO";
        case 'Transferred':
            return "TODO";
        default:
            return "";
    }
}

function trimTypeName(typeName: string | undefined) {
	let name = typeName
	if (typeName?.startsWith('Contract')) {
		name = typeName.slice(8)
	}
	return name
}
</script>
