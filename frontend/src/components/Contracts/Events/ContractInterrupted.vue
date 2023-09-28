<template>
	<div>Expand to see logs</div>
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
import { ContractInterrupted } from '../../../../src/types/generated'

type Props = {
	contractEvent: ContractInterrupted
}
const props = defineProps<Props>()
</script>
