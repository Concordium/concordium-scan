<template>
	<Tooltip
		:text="statusText || label"
		:on-mouse-enter="handleOnMouseEnter"
		:position="position"
		:text-class="tooltipClass"
	>
		<button
			:aria-label="label"
			:class="$style.w24"
			class="transition-colors text-theme-faded hover:text-theme-interactiveHover inline"
			@click="handleOnCopy"
		>
			<ClipboardIcon
				class="inline align-text-top"
				:class="props.iconSize ? props.iconSize : 'h-4'"
			/>
		</button>
	</Tooltip>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import ClipboardIcon from '~/components/icons/ClipboardIcon.vue'
import { Position } from '~~/src/composables/useTooltip'

type Props = {
	text: string
	label: string
	tooltipClass?: string
	iconSize?: string
}
const position = 'bottom' as Position

const props = defineProps<Props>()

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

<style module>
.w24 {
	width: 24px;
}
</style>
