<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>Init Name:</div>
		<div>
			{{ props.contractEvent.initName }}
		</div>
	</div>
	<div>
		<div>Module Reference:</div>
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
import { ContractInitialized } from '../../../../src/types/generated'
import LogsHEX from './LogsHEX.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	contractEvent: ContractInitialized
}
const props = defineProps<Props>()
</script>
