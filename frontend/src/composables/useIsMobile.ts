export const useIsMobile = () => {
	const isMobile = ref(false)
	onMounted(() => {
		window.addEventListener('resize', updateSize)
		window.addEventListener('orientationchange', updateSize)
		updateSize()
	})
	onUnmounted(() => {
		window.removeEventListener('resize', updateSize)
		window.removeEventListener('orientationchange', updateSize)
	})
	const updateSize = () => {
		isMobile.value = !window.matchMedia('(min-width: 1024px)').matches
	}
	return { isMobile }
}
