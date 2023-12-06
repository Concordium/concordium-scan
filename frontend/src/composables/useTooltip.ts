import { ref } from 'vue'

/**
 * Hook to control tooltip position values
 * Returns CSS values for triangle position, tooltip position and animation
 */
export const useTooltip = (xOverride?: string, yOverride?: string) => {
	const TOOLTIP_OFFSET = 10

	// Drawing of the triangle
	const triangleBottomBorder = `${TOOLTIP_OFFSET}px`

	// Positioning of the triangle
	const trianglePosTop = `-${TOOLTIP_OFFSET}px`

	// Positioning of the tooltip
	const tooltipX = ref('0px')
	const tooltipY = ref('0px')
	const triangleShift = ref('0px')
	const calculateCoordinates = (event: MouseEvent) => {
		// compiler does not know if this is e.g. a SVGElement, on which `target` does not exist
		const target = event.target as HTMLSpanElement

		const { x, y } = target.getBoundingClientRect()

		tooltipX.value = xOverride || x + 'px'
		triangleShift.value = 10 + 'px'

		tooltipY.value =
			yOverride || y + target.offsetHeight + TOOLTIP_OFFSET + 'px'
	}

	return {
		triangleBottomBorder,
		trianglePosTop,
		triangleShift,
		tooltipX,
		tooltipY,
		calculateCoordinates,
	}
}
