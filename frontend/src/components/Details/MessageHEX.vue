<template>
	<div class="w-full">
		<div>Message (HEX):
			<InfoTooltip text="Input parameters given to activity entrypoint. TODO (maybe something with HEX)"/>
		</div>
		<div class="flex">
			<code class="truncate w-36">
				{{ messageAsHex }}
			</code>
			<TextCopy
				:text="messageAsHex"
				label="Click to copy message (HEX) to clipboard"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue';
import TextCopy from '../atoms/TextCopy.vue'

type Props = {
	messageAsHex: string
}
defineProps<Props>()
</script>
