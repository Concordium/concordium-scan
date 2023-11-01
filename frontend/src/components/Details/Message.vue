<template>
	<div class="w-full">
		<div>
			Message:
			<InfoTooltip text="Input parameters given to activity entrypoint." />
		</div>
		<div class="flex">
			<code class="truncate w-36">
				{{ message }}
			</code>
			<TextCopy
				v-if="message"
				:text="message"
				label="Click to copy message to clipboard"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue'
import TextCopy from '../atoms/TextCopy.vue'

type Props = {
	message: string
}
defineProps<Props>()
</script>
