<template>
	<Tooltip
		:text="statusText || label"
		:on-mouse-enter="handleOnMouseEnter"
		:position="position"
		:text-class="tooltipClass"
	>
		<button
			:aria-label="label"
			class="transition-colors hover:text-theme-interactiveHover"
			@click="handleOnCopy"
		>
			<ClipboardIcon
				class="inline align-middle"
				:class="props.iconSize ? props.iconSize : 'h-4'"
			/>
		</button>
	</Tooltip>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import type { Position } from '~/composables/useTooltip'
import ClipboardIcon from '~/components/icons/ClipboardIcon.vue'

type Props = {
	text: string
	label: string
	tooltipClass?: string
	iconSize?: string
}

const props = defineProps<Props>()

const position = 'bottom' as Position

const statusText = ref('')

const handleOnMouseEnter = () => {
	statusText.value = ''
}

const handleOnCopy = () => {
	navigator.clipboard.writeText(props.text).then(
		() => {
			statusText.value = 'Copied!'
		},
		() => {
			statusText.value = 'The text could not be copied'
		}
	)
}
</script>
