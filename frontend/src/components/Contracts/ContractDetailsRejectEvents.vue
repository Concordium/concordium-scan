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
				<InfoTooltip text="Contract logic rejected. For an explanation of the error code(s), see ">
					<template #content>
						<a
							style="color: #48a2ae;"
							href="https://developer.concordium.software/en/mainnet/smart-contracts/tutorials/piggy-bank/deploying.html#smart-contract-errors">
							link
						</a>
					</template>
				</InfoTooltip>				
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
							<InfoTooltip text="Errors are present as enum in contract. For an explanation of the error code(s), see ">
								<template #content>
									<a
										style="color: #48a2ae;"
										href="https://developer.concordium.software/en/mainnet/smart-contracts/tutorials/piggy-bank/deploying.html#smart-contract-errors">
										link
									</a>
								</template>
							</InfoTooltip>
						</div>
						<div>
							{{ contractRejectEvent.rejectedEvent.rejectReason }}
						</div>
					</div>
					<div>
						<div>Entrypoint:
							<InfoTooltip :text="RECEIVE_NAME"/>
						</div>
						<div>
							{{ getEntrypoint(contractRejectEvent.rejectedEvent.receiveName) }}
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
import { getEntrypoint } from "./Events/contractEvents";
import { ContractRejectEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { formatTimestamp } from '~~/src/utils/format'
import { RECEIVE_NAME } from '~~/src/utils/infoTooltips'

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
