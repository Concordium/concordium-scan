<template>
	<div class="relative w-full h-[450px]" style="height: 450px">
		<canvas ref="canvasRef" />
	</div>
</template>

<script lang="ts" setup>
import {
	Chart,
	registerables,
	type ChartOptions,
	type TooltipItem,
} from 'chart.js/dist/chart.esm'

Chart.register(...registerables)

const canvasRef = ref<HTMLCanvasElement | null>(null)

interface Props {
	labels?: string[]
	barData?: number[]
	lineData?: number[]
	backgroundColors?: string[]
	hoverBackgroundColors?: string[]
	hoverBorderColor?: string
	cutout?: string | number
	labelFormatter?: (context: TooltipItem<'bar'>) => string
}

const props = defineProps<Props>()

const chartData = computed(() => ({
	labels: props.labels || [],
	datasets: [
		{
			type: 'bar',
			label: 'No of Transfer',
			data: props.barData || [],
			backgroundColor: '#2AE8B8',
			yAxisID: 'left-axis',
			order: 2,
			borderRadius: 8,
		},
		{
			type: 'line',
			label: 'Total Transfer Value ($)',
			data: props.lineData || [],
			borderColor: '#FFA3D7',
			borderWidth: 2,
			fill: false,
			yAxisID: 'right-axis',
			order: 1,
			tension: 0.4,
			pointRadius: 4,
			cubicInterpolationMode: 'monotone',
		},
	],
}))

const defaultOptions: ChartOptions<'bar'> = {
	plugins: {
		legend: {
			display: true,
			position: 'top', // Adjust position if needed
			labels: {
				usePointStyle: true, // Enables circular legends
				pointStyle: 'circle', // Ensures legends appear as circles
				padding: 10, // Adds space between legend items
				color: '#d1d5db',
				borderWidth: 0,
			},
			onClick: (e, legendItem, legend) => {
				const chart = legend.chart
				const datasetIndex = legendItem.datasetIndex
				const meta = chart.getDatasetMeta(datasetIndex)
				meta.hidden =
					meta.hidden === null
						? !chart.data.datasets[datasetIndex].hidden
						: null

				const isLeftAxisVisible = chart.isDatasetVisible(0)
				const isRightAxisVisible = chart.isDatasetVisible(1)

				chart.options.scales['left-axis'].display = isLeftAxisVisible
				chart.options.scales['right-axis'].display = isRightAxisVisible

				chart.update()
			},
		},
	},
	responsive: true,
	maintainAspectRatio: false,
	layout: {
		padding: { left: 40, right: 40, top: 10, bottom: 10 },
	},
	scales: {
		'left-axis': {
			position: 'left',
			ticks: {
				callback: value => `${value}M`,
				color: '#d1d5db',
			},
			title: {
				display: true,
				text: 'No. of Transfers', // Label for left Y-axis
				color: '#d1d5db',
				font: { size: 12, weight: 'bold' },
			},
		},
		'right-axis': {
			position: 'right',
			ticks: {
				callback: value => `${value / 1_000_000_000}B`,
				color: '#d1d5db',
			},
			title: {
				display: true,
				text: 'Total Transfer Value ($)', // Label for left Y-axis
				color: '#d1d5db',
				font: { size: 12, weight: 'bold' },
			},
		},
		x: {
			ticks: {
				color: '#d1d5db',
			},
		},
	},
}

let chartInstance: Chart<'bar'> | null = null

watch(chartData, () => {
	if (chartInstance) {
		chartInstance.data = chartData.value
		chartInstance.update()
	}
})

onMounted(() => {
	if (canvasRef.value) {
		chartInstance = new Chart<'bar'>(canvasRef.value, {
			type: 'bar',
			data: chartData.value,
			options: defaultOptions,
		})
	}
})

onUnmounted(() => {
	chartInstance?.destroy()
})
</script>
