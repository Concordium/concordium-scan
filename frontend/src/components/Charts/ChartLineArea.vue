<template>
	<div class="relative">
		<canvas ref="canvasRef" />
	</div>
</template>
<script lang="ts" setup>
import {
	Chart,
	registerables,
	type ChartOptions,
	type Scale,
} from 'chart.js/dist/chart.esm'
import { onMounted } from 'vue'
import type { TooltipItem } from 'chart.js'
import type { LabelFormatterFunc } from './ChartUtils'
import { prettyFormatBucketDuration } from '~/utils/format'

type Props = {
	xValues: string[] | undefined
	yValuesHigh?: (number | null)[]
	yValuesMid?: (number | null)[]
	yValuesLow?: (number | null)[]
	bucketWidth?: string
	labelFormatter?: LabelFormatterFunc
}
const canvasRef = ref()
Chart.register(...registerables)
const props = defineProps<Props>()

const chartData = {
	labels: props.xValues?.filter(x => !!x) || [],
	datasets: [
		{
			label: 'High',
			data: props.yValuesHigh?.filter(x => x !== undefined) || [],
			borderColor: '#1C6D55',
			fill: '1',
			tension: 0.1,
			borderWidth: 1,
			spanGaps: false,
			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#1C6D5599',
			order: 2,
		},
		{
			label: 'Avg',
			data: props.yValuesMid?.filter(x => x !== undefined) || [],
			borderColor: '#39DBAA',
			borderWidth: 3, // This is actually default.
			fill: 'false',
			tension: 0.1,
			spanGaps: false,
			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#39DBAA99',
			order: 1,
		},
		{
			label: 'Low',
			data: props.yValuesLow?.filter(x => x !== undefined) || [],
			borderColor: '#9CEDD4',
			fill: '-1',
			borderWidth: 1,
			tension: 0.1,
			spanGaps: false,
			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#9CEDD499',
			order: 3,
		},
	],
}

watch(props, () => {
	if (
		(props.yValuesHigh &&
			props.xValues &&
			props.yValuesHigh === chartData.datasets[0].data &&
			props.xValues === chartData.labels) ||
		!chartInstance
	)
		return

	chartInstance.data.labels = props.xValues
	chartInstance.data.datasets[0].data =
		props.yValuesHigh?.filter(x => x !== undefined) || []
	chartInstance.data.datasets[1].data =
		props.yValuesMid?.filter(x => x !== undefined) || []
	chartInstance.data.datasets[2].data =
		props.yValuesLow?.filter(x => x !== undefined) || []
	chartInstance.update()
})
const defaultOptions = ref({
	plugins: {
		legend: {
			display: false,
			title: {
				display: false,
			},
		},
		tooltip: {
			itemSort(a: TooltipItem<'line'>, b: TooltipItem<'line'>) {
				return (b.raw as number) - (a.raw as number)
			},
			callbacks: {
				title(context: TooltipItem<'line'>[]) {
					return new Date(context[0].label).toLocaleString()
				},
				beforeBody() {
					if (props.bucketWidth)
						return 'Interval: ' + prettyFormatBucketDuration(props.bucketWidth)
					return ''
				},
				label(context: TooltipItem<'line'>) {
					let label = context.dataset.label || ''
					if (label) {
						label += ': '
					}
					if (props.labelFormatter) return label + props.labelFormatter(context)
					else return label + context.parsed.y
				},
			},
		},
	},

	responsive: true,
	maintainAspectRatio: false,
	tooltip: {
		mode: 'label',
	},
	layout: {
		padding: {
			left: 0,
			bottom: 0,
			right: 0,
		},
	},
	interaction: {
		mode: 'nearest',
		axis: 'x',
		intersect: false,
	},
	scales: {
		x: {
			axis: 'x',
			display: false,
			grid: { display: false, drawBorder: false },
			label: { display: false },
		},
		xAxes: {
			display: false,
			ticks: {
				display: false,
			},
		},

		y: {
			beginAtZero: true,
			axis: 'y',
			display: false,
			grid: { display: false, drawBorder: false },
			ticks: {
				display: true,
				color: '#ffffff',
				mirror: true,
				position: 'right',
				padding: 0,
				margin: 10,
				labelOffset: -5,
				autoSkip: true,
			},
			padding: 0,
			margin: 0,
			afterFit: (axis: Scale) => {
				axis.paddingBottom = 0
			},
		},
		yAxes: {
			display: false,
		},
	},
})
let chartInstance: Chart
onMounted(() => {
	chartInstance = new Chart(canvasRef.value, {
		data: chartData,
		type: 'line',
		options: defaultOptions.value as ChartOptions<'line'>,
	})
})
</script>
