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
	labelClickable?: boolean
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
			position: 'left',
			labels: {
				usePointStyle: true,
				pointStyle: 'circle',
				padding: 10,
				color: '#ffffff',
				generateLabels: function (chart) {
					const data = chart.data
					const dataset = data.datasets[0]
					const backgroundColors = dataset.backgroundColor
					const borderColors = dataset.borderColor

					return (data.labels ?? []).map((label, i) => ({
						text: typeof label === 'string' ? label : '',
						fillStyle: Array.isArray(backgroundColors)
							? backgroundColors[i]
							: backgroundColors,
						strokeStyle: Array.isArray(borderColors)
							? borderColors[i]
							: borderColors,
						lineWidth: 0,
						index: i,
					}))
				},
			},
			onClick: (_e, legendItem) => {
				if (!props.labelClickable) return

				const index = legendItem.index
				if (index !== undefined) {
					const label = chartData.value.labels?.[index]
					if (label) {
						window.open(`/protocol-token/${label.toLowerCase()}`, '_blank')
					}
				}
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
	cutout: props.cutout || '80%',
	layout: {
		padding: { left: 40, right: 40, top: 10, bottom: 10 },
	},
}

const handleSliceClick = (event: MouseEvent) => {
	if (!props.labelClickable || !chartInstance) return

	const elements = chartInstance.getElementsAtEventForMode(
		event,
		'nearest',
		{ intersect: true },
		false
	)

	if (elements.length > 0) {
		const index = elements[0].index
		const label = chartInstance.data.labels?.[index] as string
		if (label) {
			window.open(`/protocol-token/${label.toLowerCase()}`, '_blank')
		}
	}
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

		if (props.labelClickable) {
			canvasRef.value.style.cursor = 'pointer'
			canvasRef.value.addEventListener('click', handleSliceClick)
		}
	}
})

onUnmounted(() => {
	if (props.labelClickable && canvasRef.value) {
		canvasRef.value.removeEventListener('click', handleSliceClick)
	}
	chartInstance?.destroy()
})
</script>
