<template>
	<section>
		<div>
			<slot name="content" />
		</div>
		<slot name="actions" />
	</section>
</template>

<script lang="ts" setup>
import { onMounted } from 'vue'

type Props = {
	isOpen: boolean
	isMobile?: boolean
}
const { currentDrawerCount } = useDrawer()
const props = defineProps<Props>()
onMounted(() => {
	if (!props.isMobile) toggleClasses(currentDrawerCount.value > 0)
})
watch(
	() => props.isOpen,
	value => {
		toggleClasses(value)
	}
)
watch(currentDrawerCount, v => {
	toggleClasses(v > 0)
})

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
