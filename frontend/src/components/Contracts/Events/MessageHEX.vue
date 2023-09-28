<template>
	<div class="w-full">
		<div>Message (HEX):</div>
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
import TextCopy from '~/components/atoms/TextCopy.vue'

type Props = {
	messageAsHex: string
}
defineProps<Props>()
</script>
