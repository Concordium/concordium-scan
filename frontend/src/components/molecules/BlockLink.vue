<template>
	<div v-if="props.hash || props.id" class="inline-block">
		<BlockIcon
			v-if="props.iconSize == 'big'"
			class="h-5 inline align-middle mr-3"
		/>
		<BlockIcon v-else class="h-4 w-4 align-text-top" />
		<LinkButton
			class="numerical"
			@blur="emitBlur"
			@click="drawer.push('block', props.hash, props.id)"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ shortenHash(props.hash) }}
			</div>
			<Tooltip v-else :text="props.hash" text-class="text-theme-body">
				{{ shortenHash(props.hash) }}
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="props.hash"
			label="Click to copy block hash to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import { shortenHash } from '~/utils/format'
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'
import BlockIcon from '~/components/icons/BlockIcon.vue'
type Props = {
	hash?: string
	id?: string
	iconSize?: string
	hideTooltip?: boolean
}
const props = defineProps<Props>()
const drawer = useDrawer()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}
</script>
