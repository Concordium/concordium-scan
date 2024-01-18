<template>
	<div class="inline-block whitespace-nowrap">
		<span v-if="props.url">
			<Tooltip
				text="This is an external URL, and its source may be potentially malicious. Only open URLs from sources that are known to be trusted"
				text-class="text-theme-body"
			>
				<span class="icon-warning">
					<WarningIcon class="h-4 align-middle mr-2" />
				</span>
				<span class="token-metadata-tool-tip text-theme-faded">
					{{ props.url }}
				</span>
			</Tooltip>
			<TextCopy
				:text="props.url"
				label="Click to copy"
				class="h-5 inline align-baseline ml-2"
				tooltip-class="font-sans"
			/>
		</span>
		<span v-else> - </span>
	</div>
</template>
<script setup lang="ts">
import TextCopy from '../atoms/TextCopy.vue'
import Tooltip from '../atoms/Tooltip.vue'
import WarningIcon from '../icons/WarningIcon.vue'

type Props = {
	url?: string
}
const props = defineProps<Props>()
</script>
<style scoped>
.icon-warning {
	color: #ffc600;
}
.token-metadata-tool-tip {
	text-align: left;
	text-overflow: ellipsis;
	overflow: hidden;
	white-space: nowrap;
	display: inline;
	@media screen and (min-width: 640px) and (max-width: 900px) {
		max-width: 350px;
	}
	@media screen and (min-width: 900px) and (max-width: 1536px) {
		max-width: 700px;
	}
	@media screen and (min-width: 1536px) {
		max-width: 900px;
	}
}
</style>
