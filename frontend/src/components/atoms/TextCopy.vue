<template>
	<button
		:title="label"
		:aria-label="label"
		class="transition-colors text-theme-interactive hover:text-theme-interactiveHover"
		@click="handleOnCopy"
	>
		<ClipboardCopyIcon class="h-5 inline align-baseline" />
	</button>
</template>

<script lang="ts" setup>
import { ClipboardCopyIcon } from '@heroicons/vue/solid'

type Props = {
	text: string
	label: string
}

const props = defineProps<Props>()

const handleOnCopy = () => {
	navigator.clipboard.writeText(props.text).then(
		() => {
			// Noop - Clipboard successfully set! 🎉
		},
		() => {
			// eslint-disable-next-line no-console
			console.error('Text could not be copied')
		}
	)
}
</script>
