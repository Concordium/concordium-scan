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
			:key="moduleRejectEvent.transactionHash"
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
				<InfoTooltip
					:text="getEventTooltip(moduleRejectEvent.rejectedEvent.__typename!)"
				/>
			</TableTd>
			<TableTd>
				<div
					v-if="
						moduleRejectEvent.rejectedEvent.__typename === 'InvalidInitMethod'
					"
				>
					<div>
						Init name:
						<InfoTooltip :text="INIT_NAME" />
					</div>
					<div>{{ moduleRejectEvent.rejectedEvent.initName }}</div>
				</div>
				<div
					v-if="
						moduleRejectEvent.rejectedEvent.__typename ===
						'InvalidReceiveMethod'
					"
				>
					<div>
						Entrypoint:
						<InfoTooltip :text="RECEIVE_NAME" />
					</div>
					<div>{{ moduleRejectEvent.rejectedEvent.receiveName }}</div>
				</div>
				<div
					v-if="
						moduleRejectEvent.rejectedEvent.__typename ===
						'ModuleHashAlreadyExists'
					"
				>
					<div></div>
				</div>
			</TableTd>
		</TableRow>
	</TableBody>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from '../Details/DateTimeWithLineBreak.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'
import type { ModuleReferenceRejectEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { convertTimestampToRelative } from '~~/src/utils/format'
import { RECEIVE_NAME, INIT_NAME } from '~~/src/utils/infoTooltips'

const { NOW } = useDateNow()

type Props = {
	moduleRejectEvents: ModuleReferenceRejectEvent[]
}
defineProps<Props>()

function getEventTooltip(eventType: string) {
	switch (eventType) {
		case 'InvalidInitMethod':
			return 'Initialize method failed or does not exist; check if contract name exists in module.'
		case 'InvalidReceiveMethod':
			return 'Receive method does not exist; check if contract name, receive name exists in module. '
		case 'ModuleHashAlreadyExists':
			return 'Module already deployed on-chain.'
		default:
			return ''
	}
}
</script>
