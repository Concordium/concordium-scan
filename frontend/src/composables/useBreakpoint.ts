// Enum value corresponds to default Tailwind breakpoints in px
export enum Breakpoint {
	XS = 1,
	SM = 640,
	MD = 768,
	LG = 1024,
	XL = 1280,
	XXL = 1536,
}

const getBreakpoint = () => {
	if (window.matchMedia(`(min-width: ${Breakpoint.XXL}px)`).matches) {
		return Breakpoint.XXL
	}

	if (window.matchMedia(`(min-width: ${Breakpoint.XL}px)`).matches) {
		return Breakpoint.XL
	}

	if (window.matchMedia(`(min-width: ${Breakpoint.LG}px)`).matches) {
		return Breakpoint.LG
	}

	if (window.matchMedia(`(min-width: ${Breakpoint.MD}px)`).matches) {
		return Breakpoint.MD
	}

	if (window.matchMedia(`(min-width: ${Breakpoint.SM}px)`).matches) {
		return Breakpoint.SM
	}

	return Breakpoint.XS
}

/**
 * Hook to determine current breakpoint
 * Returns current breakpoint as numeric enum
 */
export const useBreakpoint = () => {
	const breakpoint = ref<Breakpoint>(Breakpoint.XS)

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
		breakpoint.value = getBreakpoint()
	}

	return { breakpoint }
}
