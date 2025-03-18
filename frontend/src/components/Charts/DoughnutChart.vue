<template>
	<div class="relative">
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
			borderWidth: 1,
		},
	],
}))

const defaultOptions: ChartOptions<'doughnut'> = {
	plugins: {
		legend: {
			display: false,
		},
		tooltip: {
			callbacks: {
				label: function (context) {
					const total = context.dataset.data.reduce(
						(sum, value) => sum + (value as number),
						0
					)
					const value = context.raw as number
					const percentage = ((value / total) * 100).toFixed(2) + '%'
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
