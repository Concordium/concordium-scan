<template>
	<div class="w-full">
		<div>Logs (HEX):
			<InfoTooltip text="Logs produced by contract execution. You can copy the hex string and decode the log if the contract uses a schema."/>
		</div>
		<template v-if="eventsAsHex?.nodes?.length">
			<div v-for="(event, i) in eventsAsHex.nodes" :key="i" class="flex">
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
import { Maybe, StringConnection } from '../../types/generated'
import TextCopy from '../../components/atoms/TextCopy.vue'
import InfoTooltip from '../atoms/InfoTooltip.vue'

type Props = {
	eventsAsHex?: Maybe<StringConnection>
}
defineProps<Props>()
</script>
