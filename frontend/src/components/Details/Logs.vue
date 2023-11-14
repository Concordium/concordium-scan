<template>
	<div class="w-full">
		<div>
			Logs:
			<InfoTooltip text="Logs produced by contract execution." />
		</div>
		<template v-if="events?.nodes?.length">
			<div v-for="(event, i) in events.nodes" :key="i" class="flex">
				<code class="whitespace-pre-line">
					{{ event }}
				</code>
				<TextCopy
					:text="event"
					label="Click to copy events logs to clipboard"
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
	events?: Maybe<StringConnection>
}
defineProps<Props>()
</script>
