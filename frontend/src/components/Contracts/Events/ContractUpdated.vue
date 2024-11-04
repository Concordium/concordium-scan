<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>
			Instigator:
			<InfoTooltip text="Account or contract which initiated the activity." />
		</div>
		<div>
			<ContractLink
				v-if="props.contractEvent.instigator.__typename === 'ContractAddress'"
				:address="props.contractEvent.instigator.asString"
				:contract-address-index="props.contractEvent.instigator.index"
				:contract-address-sub-index="props.contractEvent.instigator.subIndex"
			/>
			<AccountLink
				v-else-if="
					props.contractEvent.instigator.__typename === 'AccountAddress'
				"
				:address="props.contractEvent.instigator.asString"
			/>
		</div>
	</div>
	<div>
		<div>
			Entrypoint:
			<InfoTooltip :text="RECEIVE_NAME" />
		</div>
		<div>
			{{ getEntrypoint(props.contractEvent.receiveName) }}
		</div>
	</div>
	<div v-if="props.contractEvent?.version">
		<div>Version:</div>
		<div>
			{{ props.contractEvent.version }}
		</div>
	</div>
	<Message
		v-if="props.contractEvent.message"
		:message="props.contractEvent.message"
	/>
	<MessageHEX
		v-else-if="props.contractEvent.messageAsHex"
		:message-as-hex="props.contractEvent.messageAsHex"
	/>
	<Logs
		v-if="props.contractEvent.events?.nodes?.length"
		:events="props.contractEvent.events"
	/>
	<LogsHEX
		v-else-if="props.contractEvent.eventsAsHex?.nodes?.length"
		:events-as-hex="props.contractEvent.eventsAsHex"
	/>
</template>
<script lang="ts" setup>
import MessageHEX from '../../Details/MessageHEX.vue'
import Message from '../../Details/Message.vue'
import LogsHEX from '../../Details/LogsHEX.vue'
import Logs from '../../Details/Logs.vue'
import { getEntrypoint } from './contractEvents'
import InfoTooltip from '~/components/atoms/InfoTooltip.vue'
import type { ContractUpdated } from '~~/src/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'
import { RECEIVE_NAME } from '~/utils/infoTooltips'

type Props = {
	contractEvent: ContractUpdated
}
const props = defineProps<Props>()
</script>
