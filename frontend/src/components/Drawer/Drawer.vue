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
const openScroll = ref(0)
const toggleClasses = (isOpen: boolean) => {
	const appEl = document.getElementById('app')

	const classes = ['w-full', 'overflow-hidden', 'fixed']

	if (isOpen) {
		if (!appEl?.classList.contains('fixed')) {
			openScroll.value = window.scrollY
			if (appEl !== null) appEl.style.top = `-${openScroll.value}px`
		}
		appEl?.classList.add(...classes)
	} else {
		appEl?.classList.remove(...classes)
		if (openScroll.value !== 0) window.scrollTo(0, openScroll.value)
		openScroll.value = 0
	}
}
</script>
