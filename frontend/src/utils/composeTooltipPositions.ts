type TooltipPosition = 'bottom' | 'top'

export const composeTooltipPositions = (position: TooltipPosition) => {
	if (position === 'top') {
		return {
			arrowTPos: 'unset',
			arrowBPos: '-10px',
		}
	}
	return {
		arrowTPos: '-10px',
		arrowBPos: 'unset',
	}
}
