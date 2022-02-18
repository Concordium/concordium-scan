<template>
	<LineChart
		v-bind="lineChartProps"
		ref="lineChartRef"
		:height="100"
		:width="286"
		:chart-id="props.chartId"
	/>
</template>
<script lang="ts" setup>
import { LineChart, useLineChart } from 'vue-chart-3'
import { Chart, registerables } from 'chart.js/dist/chart.esm'

type Props = {
	xValues: unknown[]
	yValues: unknown[]
	chartId: string
}

Chart.register(...registerables)
const props = defineProps<Props>()

const testData = reactive({
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
		props.yValues[0] === testData.datasets[0].data[0] &&
		props.xValues[0] === testData.labels[0]
	)
		return

	testData.labels = props.xValues
	testData.datasets[0].data = props.yValues as number[]
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
const { lineChartProps, lineChartRef } = useLineChart({
	chartData: testData,
	options: defaultOptions,
})
</script>
