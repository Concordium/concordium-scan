<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.contractUpdated.amount" />
		</div>
	</div>
	<div>
		<div>Contract Address:</div>
		<div>
			<ContractLink
				:address="contractEvent.contractUpdated.contractAddress.asString"
				:contract-address-index="
					contractEvent.contractUpdated.contractAddress.index
				"
				:contract-address-sub-index="
					contractEvent.contractUpdated.contractAddress.subIndex
				"
			/>
		</div>
	</div>
	<div>
		<div>Receive name of contract called:
			<Tooltip
				text="Entrypoint of the activity of the called contract."
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
			>
				<span style="padding-left: 10px;">?</span>
			</Tooltip>			
		</div>
		<div>
			{{ props.contractEvent.contractUpdated.receiveName }}
		</div>
	</div>
	<div>
		<div>Version:</div>
		<div>
			{{ props.contractEvent?.contractUpdated.version }}
		</div>
	</div>
	<MessageHEX
		:message-as-hex="props.contractEvent.contractUpdated.messageAsHex"
	/>
	<LogsHEX :events-as-hex="props.contractEvent.contractUpdated.eventsAsHex" />
</template>
<script lang="ts" setup>
import MessageHEX from '../../Details/MessageHEX.vue'
import LogsHEX from '../../Details/LogsHEX.vue'
import { ContractCall } from '../../../../src/types/generated'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractCall
}
const props = defineProps<Props>()
</script>
