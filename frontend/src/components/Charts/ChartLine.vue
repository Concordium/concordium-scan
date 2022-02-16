<template>
	<LineChart v-bind="lineChartProps" ref="lineRef" :height="100" />
</template>
<script lang="ts" setup>
import { LineChart, useLineChart } from 'vue-chart-3'
import { Chart, registerables } from 'chart.js/dist/chart.esm'
type Props = {
	xValues: unknown[]
	yValues: unknown[]
}
Chart.register(...registerables)
const props = defineProps<Props>()
const lineRef = ref(null)

const testData = {
	labels: props.xValues,
	datasets: [
		{
			label: '',
			data: props.yValues as number[],
			borderColor: '#39DBAA',
			fill: false,
			tension: 0.5,
			pointRadius: 0, // Disables the small points
			pointHitRadius: 0, // Disables the tooltip
			hoverBackgroundColor: '#FFFFFF',
		},
	],
}
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
const { lineChartProps } = useLineChart({
	chartData: testData,
	options: defaultOptions,
})
</script>
