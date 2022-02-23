<template>
	<div class="relative">
		<canvas ref="canvasRef"></canvas>
	</div>
</template>
<script lang="ts" setup>
import { Chart, registerables, Scale } from 'chart.js/dist/chart.esm'
import * as Chartjs from 'chart.js/dist/chart.esm'
type Props = {
	xValues: unknown[]
	yValuesHigh: unknown[]
	yValuesMid: unknown[]
	yValuesLow: unknown[]
}
const canvasRef = ref()
Chart.register(...registerables)
const props = defineProps<Props>()

const chartData = {
	labels: props.xValues,
	datasets: [
		{
			label: 'High',
			data: props.yValuesHigh as number[],
			borderColor: '#EB5837',
			fill: '1',
			tension: 0.5,

			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#95270f',
		},
		{
			label: 'Avg',
			data: props.yValuesMid as number[],
			borderColor: '#39DBAA',
			fill: 'false',
			tension: 0.5,

			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#39DBAA99',
		},
		{
			label: 'Low',
			data: props.yValuesLow as number[],
			borderColor: '#4edfb3',
			fill: '-1',
			tension: 0.5,

			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#177d5e',
		},
	],
}
/*
watch(props, () => {
	if (
		(props.yValues[0] === chartData.datasets[0].data[0] &&
			props.xValues[0] === chartData.labels[0]) ||
		!chartInstance
	)
		return

	chartInstance.data.labels = props.xValues
	chartInstance.data.datasets[0].data = props.yValues as number[]
	chartInstance.update()
}) */
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
				title(context) {
					return new Date(context[0].label).toLocaleTimeString()
				},
				label(context) {
					/* let label = context.dataset.label || ''

					if (label) {
						label += ': '
					}
					if (context.parsed.y !== null) {
						label += new Intl.NumberFormat('en-US', {
							style: 'currency',
							currency: 'USD',
						}).format(context.parsed.y)
					} */
					let label = context.dataset.label || ''
					if (label) {
						label += ': '
					}
					return label + context.parsed.y
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
			beginAtZero: false,
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
onMounted(() => {
	/* eslint-disable no-new */
	new Chartjs.Chart(canvasRef.value, {
		data: chartData,
		type: 'line',
		options: defaultOptions.value as Chartjs.ChartOptions<'line'>,
	})
})
</script>
