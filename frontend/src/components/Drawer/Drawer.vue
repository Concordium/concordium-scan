<template>
	<section>
		<button
			class="rounded absolute right-16 top-5 z-20 p-2 hover:bg-theme-button-primary-hover transition-colors"
			@click="back"
		>
			<ChevronBackIcon class="h-6" />
		</button>
		<button
			class="rounded absolute right-10 top-5 z-20 p-2 hover:bg-theme-button-primary-hover transition-colors"
			aria-label="Close"
			@click="props.onClose"
		>
			<XIcon class="h-6" />
		</button>
		<button
			v-if="canGoForward"
			class="rounded absolute right-6 top-5 z-20 p-2 hover:bg-theme-button-primary-hover transition-colors"
			@click="forward"
		>
			<ChevronForwardIcon class="h-6" />
		</button>
		<div>
			<slot name="content" />
		</div>
		<slot name="actions" />
	</section>
</template>

<script lang="ts" setup>
import { XIcon } from '@heroicons/vue/solid/index.js'
import { onMounted } from 'vue'
import ChevronForwardIcon from '~/components/icons/ChevronForwardIcon.vue'
import ChevronBackIcon from '~/components/icons/ChevronBackIcon.vue'

type Props = {
	isOpen: boolean
	onClose: () => void
	isMobile?: boolean
}
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
const props = defineProps<Props>()
onMounted(() => {
	if (!props.isMobile) toggleClasses(props.isOpen)
})
watch(
	() => props.isOpen,
	value => {
		toggleClasses(value)
	}
)
const toggleClasses = (isOpen: boolean) => {
	const appEl = document.getElementById('app')

	const classes = [
		'max-h-screen',
		'w-full',
		'overflow-hidden',
		'fixed',
		'top-0',
		'left-0',
	]

	if (isOpen) {
		appEl?.classList.add(...classes)
	} else {
		appEl?.classList.remove(...classes)
	}
}
</script>

<style module>
.drawer {
	@apply flex flex-col flex-nowrap justify-between min-h-screen w-full md:w-3/4 xl:w-1/2 absolute top-0 right-0 z-20 overflow-x-hidden;
	background: hsl(247, 40%, 18%);
	box-shadow: -25px 0 50px -12px var(--color-shadow-dark);
}
</style>
