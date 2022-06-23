import type { TooltipItem } from 'chart.js'

export type LabelFormatterFunc = (args: TooltipItem<'bar' | 'line'>) => string
