<template>
	<div ref="containerDiv" height="100" width="286"></div>
</template>
<script lang="ts" setup>
import { Chart, registerables } from 'chart.js'
import * as Chartjs from 'chart.js'
type Props = {
	xValues: unknown[]
	yValues: unknown[]
	chartId: string
}
Chart.register(...registerables)
const props = defineProps<Props>()
const containerDiv = ref()

const chartData = reactive({
	labels: props.xValues,
	datasets: [
		{
			label: '',
			data: props.yValues as number[],
			borderColor: '#39DBAA',
			fill: 'start',
			tension: 0.5,

			pointRadius: 0, // Disables the small points
			pointHitRadius: 0, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: '#39DBAA99',
		},
	],
})
watch(props, () => {
	if (
		props.yValues[0] === chartData.datasets[0].data[0] &&
		props.xValues[0] === chartData.labels[0]
	)
		return

	chartData.labels = props.xValues
	chartData.datasets[0].data = props.yValues as number[]
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
		},
		yAxes: {
			display: false,
		},
	},
})
const chartInstance = ref()
let canvasEl: HTMLCanvasElement
onMounted(() => {
	canvasEl = document.createElement('canvas')
	canvasEl.setAttribute('height', 100)
	canvasEl.setAttribute('width', 286)

	containerDiv.value.appendChild(canvasEl)
	setTimeout(() => {
		chartInstance.value = new Chartjs.Chart(canvasEl, {
			data: chartData,
			type: 'line',
			options: defaultOptions.value as Chartjs.ChartOptions<'line'>,
		})
	}, 100)
})
onBeforeUnmount(() => {
	containerDiv.value.removeChild(canvasEl)
})
</script>
