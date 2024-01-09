<template>
	<div class="inline-block whitespace-nowrap">
		<TransactionIcon class="h-4 w-4 align-text-top" />
		<span v-if="props.url">
			<LinkButton class="px-2" @click="handleOnClick">
				<Tooltip
					:text="props.url"
					text-class="text-theme-body"
					class="token-metadata-tool-tip"
				>
					{{ props.url }}
				</Tooltip>
			</LinkButton>
			<TextCopy
				:text="props.url"
				label="Click to copy"
				class="h-5 inline align-baseline"
				tooltip-class="font-sans"
			/>
		</span>
		<span v-else> - </span>
	</div>
</template>
<script setup lang="ts">
import LinkButton from '../atoms/LinkButton.vue'
import TransactionIcon from '../icons/TransactionIcon.vue'
import TextCopy from '../atoms/TextCopy.vue'
import Tooltip from '../atoms/Tooltip.vue'

type Props = {
	url?: string
}
const props = defineProps<Props>()

const handleOnClick = () => {
	props.url && window.open(props.url, '_blank')
}
</script>
<style scoped>
.token-metadata-tool-tip {
	text-align: left;
	text-overflow: ellipsis;
	overflow: hidden;
	white-space: nowrap;
	display: block;
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
