<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>Init name:
			<InfoTooltip text="Initial entrypoint of the contract."/>
		</div>
		<div>
			{{ props.contractEvent.initName }}
		</div>
	</div>
	<div>
		<div>Module:
			<InfoTooltip text="Container which holds execution code for one-or-more contracts. This reference holds the execution code of the contract at the time of the activity."/>
		</div>
		<div>
			<ModuleLink :module-reference="props.contractEvent.moduleRef" />
		</div>
	</div>
	<div v-if="props.contractEvent?.version">
		<div>Version:</div>
		<div>
			{{ props.contractEvent.version }}
		</div>
	</div>
	<LogsHEX :events-as-hex="props.contractEvent.eventsAsHex" />
</template>
<script lang="ts" setup>
import LogsHEX from '../../Details/LogsHEX.vue'
import { ContractInitialized } from '../../../../src/types/generated'
import InfoTooltip from '../../atoms/InfoTooltip.vue'
import ModuleLink from '../../molecules/ModuleLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractInitialized
}
const props = defineProps<Props>()
</script>
