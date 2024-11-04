<template>
	<div class="w-full">
		<div>
			Logs (hexadecimal):
			<InfoTooltip text="Logs produced by contract execution." />
		</div>
		<template v-if="eventsAsHex?.nodes?.length">
			<div v-for="(event, i) in eventsAsHex.nodes" :key="i" class="flex">
				<code class="truncate w-36">
					{{ event }}
				</code>
				<TextCopy
					:text="event"
					label="Click to copy events logs (hexadecimal) to clipboard"
				/>
			</div>
		</template>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue'
import type { Maybe, StringConnection } from '~/types/generated'
import TextCopy from '~/components/atoms/TextCopy.vue'

type Props = {
	eventsAsHex?: Maybe<StringConnection>
}
defineProps<Props>()
</script>
