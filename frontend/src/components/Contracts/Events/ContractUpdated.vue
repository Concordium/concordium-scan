<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>Instigator:
			<InfoTooltip text="Account or contract which initiated the activity."/>
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
		<div>Receive name:
			<InfoTooltip text="Entrypoint of the activity of the contract."/>
		</div>
		<div>
			{{ props.contractEvent.receiveName }}
		</div>
	</div>
	<div v-if="props.contractEvent?.version">
		<div>Version:</div>
		<div>
			{{ props.contractEvent.version }}
		</div>
	</div>
	<MessageHEX :message-as-hex="props.contractEvent.messageAsHex" />
	<LogsHEX :events-as-hex="props.contractEvent.eventsAsHex" />
</template>
<script lang="ts" setup>
import { ContractUpdated } from '../../../../src/types/generated'
import MessageHEX from '../../Details/MessageHEX.vue'
import LogsHEX from '../../Details/LogsHEX.vue'
import InfoTooltip from '../../atoms/InfoTooltip.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractUpdated
}
const props = defineProps<Props>()
</script>
