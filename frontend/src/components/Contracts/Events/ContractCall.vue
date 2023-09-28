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
		<div>Receive name of contract called:</div>
		<div>
			{{ props.contractEvent.contractUpdated.receiveName }}
		</div>
	</div>
	<div>
		<div>Version of Contract Called:</div>
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
import { ContractCall } from '../../../../src/types/generated'
import MessageHEX from './MessageHEX.vue'
import LogsHEX from './LogsHEX.vue'
import Amount from '~/components/atoms/Amount.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'

type Props = {
	contractEvent: ContractCall
}
const props = defineProps<Props>()
</script>
