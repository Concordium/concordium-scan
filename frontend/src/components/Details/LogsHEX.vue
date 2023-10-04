<template>
	<div class="w-full">
		<div>Logs (HEX):
			<Tooltip
				text="Logs from contract in activity. TODO (maybe something with HEX)"
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
			>
				<span style="padding-left: 10px;">?</span>
			</Tooltip>			
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
import Tooltip from '~~/src/components/atoms/Tooltip.vue'

type Props = {
	eventsAsHex?: Maybe<StringConnection>
}
defineProps<Props>()
</script>
