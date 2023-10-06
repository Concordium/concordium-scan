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
				<InfoTooltip :text="getEventTooltip(contractRejectEvent.rejectedEvent.__typename!)"/>
			</TableTd>
			<TableTd>
				<DetailsView
					v-if="
						contractRejectEvent.rejectedEvent.__typename === 'RejectedReceive'
					"
					:id="i"
				>
					<div>
						<div>Reject reason:
							<InfoTooltip text="Errors are present as enum in contract. For an explanation of the error code(s), see link https://developer.concordium.software/en/mainnet/smart-contracts/tutorials/piggy-bank/deploying.html#smart-contract-errors ."/>
						</div>
						<div>
							{{ contractRejectEvent.rejectedEvent.rejectReason }}
						</div>
					</div>
					<div>
						<div>Receive name:
							<InfoTooltip :text="RECEIVE_NAME"/>
						</div>
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
import InfoTooltip from '../atoms/InfoTooltip.vue'
import { ContractRejectEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { formatTimestamp } from '~~/src/utils/format'
import { RECEIVE_NAME } from '~~/src/utils/infoTooltips'

type Props = {
	contractRejectEvents: ContractRejectEvent[]
}
defineProps<Props>()

function getEventTooltip(eventType: string) {
	switch(eventType) {
		case 'RejectedReceive':
			return "Contract logic rejected. For an explanation of the error code(s), see link https://developer.concordium.software/en/mainnet/smart-contracts/tutorials/piggy-bank/deploying.html#smart-contract-errors.";
		default:
			return "";
	}
}

function trimTypeName(typeName: string | undefined) {
	let name = typeName
	if (typeName?.startsWith('Rejected')) {
		name = typeName.slice(8)
	}
	return name
}
</script>
