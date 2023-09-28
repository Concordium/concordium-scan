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
			v-for="(contractRejectEvent, i) in contractRejectEvents"
			:key="contractRejectEvent"
		>
			<TableTd class="numerical">
				<TransactionLink :hash="contractRejectEvent.transactionHash" />
			</TableTd>
			<TableTd>
				<Tooltip :text="formatTimestamp(contractRejectEvent.blockSlotTime)">
					<DateTimeWithLineBreak
						:date-time="contractRejectEvent.blockSlotTime"
					/>
				</Tooltip>
			</TableTd>
			<TableTd>
				{{ trimTypeName(contractRejectEvent.rejectedEvent.__typename) }}
			</TableTd>
			<TableTd>
				<DetailsView
					v-if="
						contractRejectEvent.rejectedEvent.__typename === 'RejectedReceive'
					"
					:id="i"
				>
					<div>
						<div>Reject Reason:</div>
						<div>
							{{ contractRejectEvent.rejectedEvent.rejectReason }}
						</div>
					</div>
					<div>
						<div>Receive Name:</div>
						<div>
							{{ contractRejectEvent.rejectedEvent.receiveName }}
						</div>
					</div>
					<MessageHEX
						:message-as-hex="contractRejectEvent.rejectedEvent.messageAsHex"
					/>
				</DetailsView>
			</TableTd>
		</TableRow>
	</TableBody>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from '../Details/DateTimeWithLineBreak.vue'
import MessageHEX from '../Details/MessageHEX.vue'
import DetailsView from '../Details/DetailsView.vue'
import { ContractRejectEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { formatTimestamp } from '~~/src/utils/format'

const { NOW } = useDateNow()

type Props = {
	contractRejectEvents: ContractRejectEvent[]
}
defineProps<Props>()

function trimTypeName(typeName: string | undefined) {
	let name = typeName
	if (typeName?.startsWith('Rejected')) {
		name = typeName.slice(8)
	}
	return name
}
</script>
