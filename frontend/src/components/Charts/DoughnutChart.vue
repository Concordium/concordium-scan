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
	data?: number[]
	backgroundColors?: string[]
	hoverBackgroundColors?: string[]
	hoverBorderColor?: string
	cutout?: string | number
	labelFormatter?: (context: TooltipItem<'doughnut'>) => string
}

const props = defineProps<Props>()

const chartData = computed(() => ({
	labels: props.labels || [],
	datasets: [
		{
			data: props.data || [],
			borderWidth: 0,
			backgroundColor: props.backgroundColors || [
				'#39DBAA',
				'#FF6384',
				'#36A2EB',
				'#FFCE56',
			],
			hoverBackgroundColor: props.hoverBackgroundColors || [
				'#2CA081',
				'#D4375D',
				'#2B8DC9',
			],
			hoverBorderColor: props.hoverBorderColor || '#FFFFFF',
			hoverOffset: 4,
		},
	],
}))

const defaultOptions: ChartOptions<'doughnut'> = {
	plugins: {
		legend: {
			display: true,
			position: 'left', // Adjust position if needed
			labels: {
				usePointStyle: true, // Enables circular legends
				pointStyle: 'circle', // Ensures legends appear as circles
				padding: 10, // Adds space between legend items
				color: '#ffffff',
			},
		},
		tooltip: {
			callbacks: {
				label: function (context) {
					const datasetData = context.dataset.data as number[]
					const total = datasetData.length
						? datasetData.reduce((sum, value) => Number(sum) + Number(value), 0)
						: 1 // Prevent division by zero

					const value = context.raw as number
					const percentage =
						total > 0 ? ((value / total) * 100).toFixed(2) + '%' : '0%'

					return props.labelFormatter
						? props.labelFormatter(context)
						: `${context.label}: ${context.parsed} (${percentage})`
				},
			},
		},
	},
	responsive: true,
	maintainAspectRatio: false,
	cutout: props.cutout || '60%',
	layout: {
		padding: { left: 40, right: 40, top: 10, bottom: 10 },
	},
}

let chartInstance: Chart<'doughnut'> | null = null

watch(chartData, () => {
	if (chartInstance) {
		chartInstance.data = chartData.value
		chartInstance.update()
	}
})

onMounted(() => {
	if (canvasRef.value) {
		chartInstance = new Chart<'doughnut'>(canvasRef.value, {
			type: 'doughnut',
			data: chartData.value,
			options: defaultOptions,
		})
	}
})

onUnmounted(() => {
	chartInstance?.destroy()
})
</script>
