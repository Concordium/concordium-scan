<template>
	<div v-if="props.address" class="inline-block whitespace-nowrap">
		<UserIcon
			v-if="props.iconSize == 'big'"
			class="h-5 inline align-text-top mr-3"
		/>
		<UserIcon v-else class="h-4 text-theme-white inline align-text-top" />
		<LinkButton
			class="numerical px-2"
			@blur="emitBlur"
			@click="() => handleOnClick(props.address)"
		>
			<div v-if="props.hideTooltip" text-class="text-theme-body">
				{{ props.address }}
			</div>
			<Tooltip v-else :text="props.address" text-class="text-theme-body">
				{{ props.address }}
			</Tooltip>
		</LinkButton>
		<TextCopy
			:text="props.address"
			label="Click to copy address to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>
<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	address?: string | null
	iconSize?: string
	hideTooltip?: boolean
}
const props = defineProps<Props>()
const drawer = useDrawer()
const emit = defineEmits(['blur'])
const emitBlur = (newTarget: FocusEvent) => {
	emit('blur', newTarget)
}

const handleOnClick = (address?: string | null) => {
	address && drawer.push({ entityTypeName: 'contract', address })
}
</script>
