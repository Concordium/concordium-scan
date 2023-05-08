<template>
	<div class="inline-block whitespace-nowrap">
		<TransactionIcon class="h-4 w-4 align-text-top" />
		<span v-if="props.url">
			<LinkButton
				class="numerical px-2"
				@blur="emitBlur"
				@click="() => handleOnClick(props.url)"
			>
				<div v-if="props.hideTooltip" text-class="text-theme-body" class="text">
					{{ shortenString(props.data, props.length, props.suffix) }}
				</div>
				<Tooltip
					v-else
					:text="props.data"
					text-class="text-theme-body"
					class="text"
				>
					{{ shortenString(props.data, props.length, props.suffix) }}
				</Tooltip>
			</LinkButton>
			<TextCopy
				v-if="props.url"
				:text="props.url"
				label="Click to copy"
				class="h-5 inline align-baseline"
				tooltip-class="font-sans"
			/>
		</span>
		<span v-else>
			<LinkButton class="numerical px-2 text">
				{{ props.data }}
			</LinkButton>
		</span>
	</div>
</template>
<script lang="ts" setup>
import { shortenString } from '~/utils/format'
import LinkButton from '~/components/atoms/LinkButton.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
type Props = {
	url?: string
	data?: string
	hideTooltip?: boolean
	suffix?: string
	length?: number
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
<style>
.text {
	white-space: nowrap;
	text-overflow: ellipsis;
	overflow: hidden;
}
</style>
