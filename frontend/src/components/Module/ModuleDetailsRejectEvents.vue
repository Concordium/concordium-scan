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
		<TableRow
			v-for="moduleRejectEvent in moduleRejectEvents"
			:key="moduleRejectEvent"
		>
			<TableTd class="numerical">
				<TransactionLink :hash="moduleRejectEvent.transactionHash" />
			</TableTd>
			<TableTd>
				<Tooltip
					:text="
						convertTimestampToRelative(moduleRejectEvent.blockSlotTime, NOW)
					"
				>
					<DateTimeWithLineBreak :date-time="moduleRejectEvent.blockSlotTime" />
				</Tooltip>
			</TableTd>
			<TableTd>
				{{ moduleRejectEvent.rejectedEvent.__typename }}
				<Tooltip
					:text="getEventTooltip(moduleRejectEvent.rejectedEvent.__typename!)"
					position="bottom"
					x="50%"
					y="50%"
					tooltip-position="absolute"
				>
					<span style="padding-left: 10px;">?</span>
				</Tooltip>		
			</TableTd>
			<TableTd>
				<div v-if="moduleRejectEvent.rejectedEvent.__typename === 'InvalidInitMethod'">
					<div>Init name:
						<Tooltip
							text="Initial entrypoint of the contract."
							position="bottom"
							x="50%"
							y="50%"
							tooltip-position="absolute"
						>
							<span style="padding-left: 10px;">?</span>
						</Tooltip>						
					</div>
					<div>{{ moduleRejectEvent.rejectedEvent.initName }}</div>
				</div>
				<div v-if="moduleRejectEvent.rejectedEvent.__typename === 'InvalidReceiveMethod'">
					<div>Receive name:
						<Tooltip
							text="Entrypoint of the activity of the contract."
							position="bottom"
							x="50%"
							y="50%"
							tooltip-position="absolute"
						>
							<span style="padding-left: 10px;">?</span>
						</Tooltip>			
					</div>
					<div>{{ moduleRejectEvent.rejectedEvent.receiveName }}</div>
				</div>
				<div v-if="moduleRejectEvent.rejectedEvent.__typename ==='ModuleHashAlreadyExists'">
					<div></div>
				</div>
			</TableTd>
		</TableRow>
	</TableBody>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from '../Details/DateTimeWithLineBreak.vue'
import { ModuleReferenceRejectEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { convertTimestampToRelative } from '~~/src/utils/format'

const { NOW } = useDateNow()

type Props = {
	moduleRejectEvents: ModuleReferenceRejectEvent[]
}
defineProps<Props>()

function getEventTooltip(eventType: string) {
	if (eventType === 'InvalidInitMethod') {
		return "TODO"
	}
	if (eventType === 'InvalidReceiveMethod') {
		return "TODO"
	}
	if (eventType === 'ModuleHashAlreadyExists') {
		return "TODO"
	}		
	return ""
}

</script>
