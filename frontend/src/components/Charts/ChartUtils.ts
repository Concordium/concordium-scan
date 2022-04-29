import type { TooltipItem } from 'chart.js'
export type LabelFormatterFunc = (
	args: TooltipItem<'bar'> | TooltipItem<'line'>
) => string
