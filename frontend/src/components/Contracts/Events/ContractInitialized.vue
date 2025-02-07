<template>
	<div>
		<div>Amount:</div>
		<div>
			<Amount :amount="props.contractEvent.amount" />
		</div>
	</div>
	<div>
		<div>
			Module:
			<InfoTooltip
				:text="`${MODULE} This reference holds the execution code of the contract at the time of the activity.`"
			/>
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
	<Logs
		v-if="props.contractEvent.events?.nodes?.length"
		:events="props.contractEvent.events"
	/>
	<LogsHEX
		v-if="props.contractEvent.eventsAsHex?.nodes?.length"
		:events-as-hex="props.contractEvent.eventsAsHex"
	/>
</template>
<script lang="ts" setup>
import LogsHEX from '../../Details/LogsHEX.vue'
import Logs from '../../Details/Logs.vue'
import InfoTooltip from '../../atoms/InfoTooltip.vue'
import ModuleLink from '../../molecules/ModuleLink.vue'
import type { ContractInitialized } from '~~/src/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import { MODULE } from '~~/src/utils/infoTooltips'

type Props = {
	contractEvent: ContractInitialized
}
const props = defineProps<Props>()
</script>
