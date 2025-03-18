<template>
	<div class="relative w-full h-[300px]" style="height: 300px">
		<!-- Adjust height -->
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
			spanGaps: true,
			borderRadius: 0,
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
	chartInstance.resize() // Ensure resizing happens

	chartInstance.update()
})

const defaultOptions = ref({
	indexAxis: 'y', // Makes the bar chart horizontal
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
					return context[0].label
				},
				beforeBody() {
					if (props.bucketWidth)
						return 'Interval: ' + prettyFormatBucketDuration(props.bucketWidth)
					return ''
				},
				label(context: TooltipItem<'bar'>) {
					if (props.labelFormatter) return props.labelFormatter(context)
					else return context.parsed.x + ''
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
		padding: { left: 10, right: 10, top: 10, bottom: 10 }, // Add spacing
	},
	interaction: {
		mode: 'nearest',
		axis: 'x',
		intersect: false,
	},
	scales: {
		x: {
			display: false,
			grid: { display: false, drawBorder: false },
			ticks: {
				autoSkip: false, // Ensure all x-axis labels are shown
			},
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
				margin: 2,
				labelOffset: -5,
				autoSkip: false,
				suggestedMin: 0,
			},
			padding: 0,
			margin: 0,
			afterFit: (axis: Scale) => {
				axis.paddingBottom = 0
			},
		},
		yAxes: {
			display: true,
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
