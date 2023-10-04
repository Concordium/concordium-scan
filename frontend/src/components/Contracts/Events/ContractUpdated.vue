<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>Instigator:
			<Tooltip
				text="Account or contract which initiated the activity."
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
			>
				<span style="padding-left: 10px;">?</span>
			</Tooltip>		
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
		<div>
			{{ props.contractEvent.receiveName }}
		</div>
	</div>
	<div>
		<div>Version:</div>
		<div>
			{{ props.contractEvent?.version }}
		</div>
	</div>
	<MessageHEX :message-as-hex="props.contractEvent.messageAsHex" />
	<LogsHEX :events-as-hex="props.contractEvent.eventsAsHex" />
</template>
<script lang="ts" setup>
import { ContractUpdated } from '../../../../src/types/generated'
import MessageHEX from '../../Details/MessageHEX.vue'
import LogsHEX from '../../Details/LogsHEX.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ContractLink from '~/components/molecules/ContractLink.vue'
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'

type Props = {
	contractEvent: ContractUpdated
}
const props = defineProps<Props>()
</script>
