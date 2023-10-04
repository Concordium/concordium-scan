<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>Init name:
			<Tooltip
				text="Initial entrypoint of the contract."
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
			>
				<span style="padding-left: 10px;">?</span>
			</Tooltip>
		</div>
		<div>
			{{ props.contractEvent.initName }}
		</div>
	</div>
	<div>
		<div>Module:
			<Tooltip
				text="Container which holds execution code for one-or-more contracts. This reference holds the execution code of the contract at the time of the activity."
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
			>
				<span style="padding-left: 10px;">?</span>
			</Tooltip>
		</div>
		<div>
			<ModuleLink :module-reference="props.contractEvent.moduleRef" />
		</div>
	</div>
	<div>
		<div>Version:</div>
		<div>
			{{ props.contractEvent?.version }}
		</div>
	</div>
	<LogsHEX :events-as-hex="props.contractEvent.eventsAsHex" />
</template>
<script lang="ts" setup>
import LogsHEX from '../../Details/LogsHEX.vue'
import { ContractInitialized } from '../../../../src/types/generated'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractInitialized
}
const props = defineProps<Props>()
</script>
