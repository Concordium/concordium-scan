<template>
	<div class="w-full">
		<div>Message (HEX):
			<Tooltip
				text="Input parameters given to activity entrypoint. TODO (maybe something with HEX)"
				position="bottom"
				x="50%"
				y="50%"
				tooltip-position="absolute"
			>
				<span style="padding-left: 10px;">?</span>
			</Tooltip>					
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
import TextCopy from '../atoms/TextCopy.vue'

type Props = {
	messageAsHex: string
}
defineProps<Props>()
</script>
