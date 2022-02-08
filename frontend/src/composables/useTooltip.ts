export type Position = 'top' | 'bottom'

/**
 * Hook to control tooltip position values
 * Returns CSS values for triangle position, tooltip position and animation
 */
export const useTooltip = (position: Position = 'top') => {
	const TOOLTIP_OFFSET = '10px'

	// Drawing of the triangle
	const triangleTopBorder = ref(TOOLTIP_OFFSET)
	const triangleBottomBorder = ref('unset')

	// Positioning of the triangle
	const trianglePosTop = ref('100%')

	// Positioning of the tooltip
	const tooltipPosYFrom = ref('-100% - 5px')
	const tooltipPosYTo = ref(`-100% - ${TOOLTIP_OFFSET}`)

	if (position === 'bottom') {
		triangleTopBorder.value = 'unset'
		triangleBottomBorder.value = TOOLTIP_OFFSET

		trianglePosTop.value = `-${TOOLTIP_OFFSET}`

		tooltipPosYFrom.value = '50% + 5px'
		tooltipPosYTo.value = `50% + ${TOOLTIP_OFFSET}`
	}

	return {
		triangleTopBorder,
		triangleBottomBorder,
		trianglePosTop,
		tooltipPosYFrom,
		tooltipPosYTo,
	}
}
