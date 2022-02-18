<template>
	<div ref="containerDiv"></div>
</template>
<script lang="ts" setup>
import { Chart, registerables } from 'chart.js/dist/chart.esm'
import * as Chartjs from 'chart.js/dist/chart.esm'
type Props = {
	xValues: unknown[]
	yValues: unknown[]
	chartHeight: number
	chartWidth: number
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
const firstFrame = ref(false)
let canvasEl: HTMLCanvasElement
onMounted(() => {
	canvasEl = document.createElement('canvas')
	canvasEl.setAttribute('height', props.chartHeight.toString())
	canvasEl.setAttribute('width', props.chartWidth.toString())

	containerDiv.value.appendChild(canvasEl)
	firstFrame.value = true
})
onUpdated(() => {
	if (firstFrame && firstFrame.value) {
		chartInstance.value = new Chartjs.Chart(canvasEl, {
			data: chartData,
			type: 'line',
			options: defaultOptions.value as Chartjs.ChartOptions<'line'>,
		})
		firstFrame.value = false
	}
})
onBeforeUnmount(() => {
	chartInstance.value.destroy()
	containerDiv.value.removeChild(canvasEl)
})
</script>
