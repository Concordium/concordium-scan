﻿<template>
	<div class="relative">
		<canvas ref="canvasRef" />
	</div>
</template>
<script lang="ts" setup>
import {
	Chart,
	registerables,
	type Scale,
	type ChartOptions,
	type TooltipItem,
} from 'chart.js/dist/chart.esm'
import type { LabelFormatterFunc } from './ChartUtils'
import { prettyFormatBucketDuration } from '~/utils/format'

type Props = {
	xValues?: string[]
	yValues?: (number | null)[]
	bucketWidth?: string
	beginAtZero?: boolean
	labelFormatter?: LabelFormatterFunc
}
const canvasRef = ref()
Chart.register(...registerables)
const props = defineProps<Props>()

const chartData = {
	labels: props.xValues?.filter(x => !!x) || [],
	datasets: [
		{
			label: '',
			data: props.yValues?.filter(x => x !== undefined) || [],
			borderColor: '#39DBAA',
			fill: 'start',
			tension: 0.1,
			spanGaps: false,
			borderRadius: 4,
			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#39DBAA',
		},
	],
}

watch(props, () => {
	if (
		(props.yValues &&
			props.xValues &&
			props.yValues === chartData.datasets[0].data &&
			props.xValues === chartData.labels) ||
		!chartInstance
	)
		return

	chartInstance.data.labels = props.xValues
	chartInstance.data.datasets[0].data =
		props.yValues?.filter(x => x !== undefined) || []
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
			callbacks: {
				title(context: TooltipItem<'bar'>[]) {
					return new Date(context[0].label).toLocaleString()
				},
				beforeBody() {
					if (props.bucketWidth)
						return 'Interval: ' + prettyFormatBucketDuration(props.bucketWidth)
					return ''
				},
				label(context: TooltipItem<'bar'>) {
					if (props.labelFormatter) return props.labelFormatter(context)
					else return context.parsed.y + ''
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
			beginAtZero: props.beginAtZero,
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
				suggestedMin: 0,
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
		type: 'bar',
		options: defaultOptions.value as ChartOptions<'bar'>,
	})
})
</script>
