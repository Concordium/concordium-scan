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
let openScroll = 0
const toggleClasses = (isOpen: boolean) => {
	const appEl = document.getElementById('app')

	const classes = ['w-full', 'overflow-hidden', 'fixed']

	if (isOpen) {
		if (!appEl?.classList.contains('fixed')) {
			openScroll = window.scrollY
			if (appEl !== null) appEl.style.top = `-${openScroll}px`
		}
		appEl?.classList.add(...classes)
	} else {
		appEl?.classList.remove(...classes)
		if (openScroll !== 0) window.scrollTo(0, openScroll)
		openScroll = 0
	}
}
</script>
