<template>
	<div class="relative w-full h-[450px]" style="height: 450px">
		<!-- Adjust height -->
		<canvas ref="canvasRef" width="450" height="450" />
	</div>
</template>
<script lang="ts" setup>
import { Chart, registerables, type TooltipItem } from 'chart.js/dist/chart.esm'
import type { LabelFormatterFunc } from './ChartUtils'
import { prettyFormatBucketDuration } from '~/utils/format'

type Props = {
	xValues?: string[]
	yValues?: (number | null)[]
	bucketWidth?: string
	beginAtZero?: boolean
	labelFormatter?: LabelFormatterFunc
	showSign?: string
}
const canvasRef = ref()
Chart.register(...registerables)
const props = defineProps<Props>()

const chartData = {
	labels: props.xValues?.filter(x => !!x) || [],
	datasets: [
		{
			label: '',
			borderWidth: 0,
			data: props.yValues?.filter(x => x !== undefined) || [],
			fill: 'start',
			tension: 0.1,
			spanGaps: false,
			borderRadius: 8,
			pointRadius: 0, // Disables the small points
			hoverBackgroundColor: '#FFFFFF',
			backgroundColor: [
				'#2AE8B8', // Bright Mint
				'#3C8AFF', // Vivid Blue
				'#FFD116', // Gold Yellow
				'#FFB21D', // Rich Amber
				'#4FD1FF', // Aqua Blue
				'#1CC6AE', // Electric Teal
				'#A393FF', // Periwinkle
				'#FF6B6B', // Coral Red
				'#D9D9D9', // Silver Grey
				'#FFA3D7', // Soft Pink
			],
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
	indexAxis: 'y', // Horizontal bar chart
	plugins: {
		legend: {
			display: false,
		},
		datalabels: {
			anchor: 'end',
			align: 'top',
			color: '#fff',
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
	layout: {
		padding: { left: 40, right: 50, top: 10, bottom: 10 },
	},
	interaction: {
		mode: 'nearest',
		axis: 'x',
		intersect: false,
	},
	scales: {
		x: {
			grid: {
				display: false, // Ensure horizontal grid lines are visible
				color: 'red', // Grid line color (change as needed)
				lineWidth: 2, // Make grid lines more visible
			},
			ticks: {
				display: false, // Hide x-axis labels, but keep grid lines
			},
		},
		y: {
			beginAtZero: props.beginAtZero,
			grid: {
				display: true, // Ensure horizontal grid lines are visible
				color: '#d1d5db', // Grid line color (change as needed)
				lineWidth: 0.1, // Make grid lines more visible
			}, // Hide vertical grid lines
			ticks: {
				display: true, // Ensure Y-axis labels are visible
				color: '#d1d5db',
				autoSkip: false,
			},
		},
	},
})

const formatNumber = (num?: number): string => {
	if (typeof num !== 'number' || isNaN(num)) return `${props.showSign}0`

	const format = (value: number, suffix: string) =>
		value % 1 === 0
			? `${props.showSign}${value}${suffix}`
			: `${props.showSign}${value.toFixed(1)}${suffix}`

	return num >= 1e12
		? format(num / 1e12, 'T')
		: num >= 1e9
		? format(num / 1e9, 'B')
		: num >= 1e6
		? format(num / 1e6, 'M')
		: num >= 1e3
		? format(num / 1e3, 'K')
		: `${props.showSign}${num}`
}

let chartInstance: Chart
onMounted(() => {
	chartInstance = new Chart(canvasRef.value, {
		type: 'bar',
		data: chartData,
		options: {
			...defaultOptions.value,
			animation: {
				onComplete: function () {
					if (!chartInstance) return

					const ctx = chartInstance.ctx
					if (!ctx) return

					ctx.font = '11px'
					ctx.fillStyle = '#d1d5db'
					ctx.textAlign = 'center'
					ctx.textBaseline = 'bottom'

					chartInstance.data.datasets.forEach((dataset, i) => {
						const meta = chartInstance.getDatasetMeta(i)
						meta.data.forEach((bar, index) => {
							const rawValue = dataset.data[index]
							if (rawValue !== null) {
								const formattedValue = formatNumber(rawValue)
								const x = bar.x + 10 + 10
								const y = bar.y + 5
								ctx.fillText(formattedValue, x, y)
							}
						})
					})
				},
			},
		},
	})

	// Ensure chartInstance is properly assigned
	chartInstance.update()
})
</script>
