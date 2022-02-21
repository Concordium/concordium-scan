<template>
	<div class="relative">
		<canvas ref="canvasRef"></canvas>
	</div>
</template>
<script lang="ts" setup>
import { Chart, registerables } from 'chart.js/dist/chart.esm'
import * as Chartjs from 'chart.js/dist/chart.esm'
type Props = {
	xValues: unknown[]
	yValues: unknown[]
}
const canvasRef = ref()
Chart.register(...registerables)
const props = defineProps<Props>()

const chartData = {
	labels: props.xValues,
	datasets: [
		{
			label: '',
			data: props.yValues as number[],
			borderColor: '#39DBAA',
			fill: 'start',
			tension: 0.5,

			pointRadius: 0, // Disables the small points
			// pointHitRadius: 10, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#39DBAA99',
		},
	],
}
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
})
const defaultOptions = ref({
	plugins: {
		legend: {
			display: false,
			title: {
				display: false,
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
			afterFit: axis => {
				axis.paddingRight = 0
				axis.paddingLeft = 0
				axis.paddingBottom = 0
			},
		},
		xAxes: {
			display: false,
			ticks: {
				display: false,
			},
			afterFit: axis => {
				axis.paddingRight = 0
				axis.paddingLeft = 0
				axis.paddingBottom = 0
			},
		},

		y: {
			beginAtZero: false,
			axis: 'y',
			display: true,
			grid: { display: false, drawBorder: false },
			ticks: {
				mirror: true,
			},
			afterFit: axis => {
				axis.paddingRight = 0
				axis.paddingLeft = 0
				axis.paddingBottom = 0
			},
		},
		yAxes: {
			display: false,
			afterFit: axis => {
				axis.paddingRight = 0
				axis.paddingLeft = 0
				axis.paddingBottom = 0
			},
		},
	},
})
let chartInstance: Chart
const firstFrame = ref(false)
onMounted(() => {
	firstFrame.value = true
})
onUpdated(() => {
	if (firstFrame && firstFrame.value) {
		chartInstance = new Chartjs.Chart(canvasRef.value, {
			data: chartData,
			type: 'line',
			options: defaultOptions.value as Chartjs.ChartOptions<'line'>,
		})
		firstFrame.value = false
	}
})
</script>
