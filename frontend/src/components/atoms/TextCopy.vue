<template>
	<Tooltip :text="statusText || label" :on-mouse-enter="handleOnMouseEnter">
		<button
			:aria-label="label"
			class="transition-colors text-theme-interactive hover:text-theme-interactiveHover"
			@click="handleOnCopy"
		>
			<ClipboardCopyIcon class="h-5 inline align-baseline" />
		</button>
	</Tooltip>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { ClipboardCopyIcon } from '@heroicons/vue/solid'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	text: string
	label: string
}

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
