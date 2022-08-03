<template>
	<div class="inline-block whitespace-nowrap">
		<TransactionIcon class="h-4 w-4 align-text-top" />
		<LinkButton
			class="numerical px-2"
			@blur="emitBlur"
			@click="() => handleOnClick(props.url)"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ shortenTokenId(props.tokenId) }}
			</div>
			<Tooltip v-else :text="props.tokenId" text-class="text-theme-body">
				{{ shortenTokenId(props.tokenId) }}
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="props.url"
			label="Click to copy token metadata url to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import { shortenTokenId } from '~/utils/format'
import LinkButton from '~/components/atoms/LinkButton.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
type Props = {
	url?: string
	tokenId: string
	hideTooltip?: boolean
}
const props = defineProps<Props>()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = (url: string | undefined) => {
	url && window.open(url, '_blank')
}
</script>
