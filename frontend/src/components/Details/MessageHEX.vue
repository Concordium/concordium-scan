<template>
	<div class="w-full">
		<div>
			Message (hexadecimal):
			<InfoTooltip
				text="Input parameters given to activity entrypoint. You can copy the hexadecimal string and decode the message if the contract uses a schema."
			/>
		</div>
		<div class="flex">
			<code class="truncate w-36">
				{{ messageAsHex }}
			</code>
			<TextCopy
				v-if="messageAsHex"
				:text="messageAsHex"
				label="Click to copy message (hexadecimal) to clipboard"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue'
import TextCopy from '../atoms/TextCopy.vue'

type Props = {
	messageAsHex: string
}
defineProps<Props>()
</script>
