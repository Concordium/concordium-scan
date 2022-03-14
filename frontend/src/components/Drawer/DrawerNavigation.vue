<template>
	<div class="absolute right-6 top-5 z-20 p-2 flex gap-4">
		<button
			class="rounded hover:bg-theme-button-primary-hover transition-colors px-1"
			aria-label="Back"
			@click="back"
		>
			<ChevronBackIcon class="h-6 align-middle" />
		</button>

		<button
			v-if="canGoForward"
			class="rounded hover:bg-theme-button-primary-hover transition-colors px-1"
			aria-label="Forward"
			@click="forward"
		>
			<ChevronForwardIcon class="h-6 align-middle" />
		</button>
		<button
			class="rounded hover:bg-theme-button-primary-hover transition-colors"
			aria-label="Close"
			@click="close"
		>
			<XIcon class="h-6" />
		</button>
	</div>
</template>
<script setup lang="ts">
import { XIcon } from '@heroicons/vue/solid/index.js'
import ChevronForwardIcon from '~/components/icons/ChevronForwardIcon.vue'
import ChevronBackIcon from '~/components/icons/ChevronBackIcon.vue'
const { softReset, currentDepth, canGoForward } = useDrawer()
const router = useRouter()
const back = () => {
	// Depth is only 1 if it was a direct link to the drawer
	if (currentDepth() > 1) router.go(-1)
	else softReset()
}
const forward = () => {
	if (canGoForward) router.go(1)
}
const close = () => {
	softReset()
}
</script>
