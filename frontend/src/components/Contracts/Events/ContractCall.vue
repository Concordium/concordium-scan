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
		<div>
			Entrypoint of contract called:
			<InfoTooltip text="Entrypoint of the activity of the called contract." />
		</div>
		<div>
			{{ getEntrypoint(props.contractEvent.contractUpdated.receiveName) }}
		</div>
	</div>
	<div v-if="props.contractEvent?.contractUpdated?.version">
		<div>Version:</div>
		<div>
			{{ props.contractEvent.contractUpdated.version }}
		</div>
	</div>
	<Message
		v-if="props.contractEvent.contractUpdated.message"
		:message="props.contractEvent.contractUpdated.message"
	/>
	<MessageHEX
		v-else-if="props.contractEvent.contractUpdated.messageAsHex"
		:message-as-hex="props.contractEvent.contractUpdated.messageAsHex"
	/>
	<Logs
		v-if="props.contractEvent.contractUpdated.events?.nodes?.length"
		:events="props.contractEvent.contractUpdated.events"
	/>
	<LogsHEX
		v-else-if="props.contractEvent.contractUpdated.eventsAsHex?.nodes?.length"
		:events-as-hex="props.contractEvent.contractUpdated.eventsAsHex"
	/>
</template>
<script lang="ts" setup>
import MessageHEX from '../../Details/MessageHEX.vue'
import Message from '../../Details/Message.vue'
import LogsHEX from '../../Details/LogsHEX.vue'
import Logs from '../../Details/Logs.vue'
import { ContractCall } from '../../../../src/types/generated'
import InfoTooltip from '../../atoms/InfoTooltip.vue'
import { getEntrypoint } from './contractEvents'
import Amount from '~/components/atoms/Amount.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'

type Props = {
	contractEvent: ContractCall
}
const props = defineProps<Props>()
</script>
