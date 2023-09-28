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
	<div class="w-full">
		<div>Logs (HEX):</div>
		<template v-if="props.contractEvent.eventsAsHex?.nodes?.length">
			<div
				v-for="(event, i) in props.contractEvent.eventsAsHex.nodes"
				:key="i"
				class="flex"
			>
				<code class="truncate w-36">
					{{ event }}
				</code>
				<TextCopy
					:text="event"
					label="Click to copy events logs (HEX) to clipboard"
				/>
			</div>
		</template>
	</div>
</template>
<script lang="ts" setup>
import { ContractInitialized } from '../../../../src/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'

type Props = {
	contractEvent: ContractInitialized
}
const props = defineProps<Props>()
</script>
