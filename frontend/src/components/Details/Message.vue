<template>
	<div class="w-full">
		<div>
			Message:
			<InfoTooltip text="Input parameters given to activity entrypoint." />
		</div>
		<div class="flex">
			<code class="deserialized-message">
				{{ message }}
			</code>
			<button
				class="relative transition-colors text-theme-faded hover:text-theme-interactiveHover inline"
				style="width: 24px"
				@click="toggleModalVisible"
			>
				<ArrowsPointingOut class="inline align-text-top h-4" />
			</button>
			<Modal v-show="isVisible" @close="closeModal" />
			<TextCopy
				v-if="message"
				:text="message"
				label="Click to copy message to clipboard"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import InfoTooltip from '../atoms/InfoTooltip.vue'
import TextCopy from '../atoms/TextCopy.vue'
import Modal from './Modal.vue'
import ArrowsPointingOut from '~/components/icons/ArrowsPointingOut.vue'

type Props = {
	message: string
}
defineProps<Props>()

const isVisible = ref(false)
const toggleModalVisible = () => {
	isVisible.value = !isVisible.value
}
const closeModal = () => {
	isVisible.value = false
}
</script>
