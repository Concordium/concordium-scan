import { ref } from 'vue'

export type Position = 'top' | 'bottom'

/**
 * Hook to control tooltip position values
 * Returns CSS values for triangle position, tooltip position and animation
 */
export const useTooltip = (
	position: Position = 'top',
	xOverride?: string,
	yOverride?: string
) => {
	const TOOLTIP_OFFSET = 10

	// Drawing of the triangle
	const triangleTopBorder = position === 'top' ? `${TOOLTIP_OFFSET}px` : 'unset'
	const triangleBottomBorder =
		position === 'bottom' ? `${TOOLTIP_OFFSET}px` : 'unset'

	// Positioning of the triangle
	const trianglePosTop = position === 'top' ? '100%' : `-${TOOLTIP_OFFSET}px`

	// Positioning of the tooltip
	const tooltipX = ref('0px')
	const tooltipY = ref('0px')
	const tooltipTransformYTo = ref('0px')

	const calculateCoordinates = (event: MouseEvent) => {
		// compiler does not know if this is e.g. a SVGElement, on which `target` does not exist
		const target = event.target as HTMLSpanElement

		const { x, y } = target.getBoundingClientRect()

		tooltipX.value = xOverride || x + 0.5 * target.offsetWidth + 'px'

		tooltipY.value =
			yOverride ||
			(position === 'top'
				? y - 0.5 * target.offsetHeight + TOOLTIP_OFFSET + 'px'
				: y + 0.5 * target.offsetHeight - TOOLTIP_OFFSET + 'px')

		// Animation values of the tooltip
		tooltipTransformYTo.value =
			position === 'top'
				? `-100% - ${0.5 * TOOLTIP_OFFSET}px`
				: target.offsetHeight + TOOLTIP_OFFSET + 'px'
	}

	return {
		triangleTopBorder,
		triangleBottomBorder,
		trianglePosTop,
		tooltipX,
		tooltipY,
		tooltipTransformYTo,
		calculateCoordinates,
	}
}
