<template>
	<MetricCard class="justify-between">
		<header class="flex flex-col items-center">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight" />
			</div>

			<div class="absolute bottom-3 text-xs text-center">
				<slot name="bottom" />
			</div>

			<div class="text-sm text-theme-faded pt-4 w-72 text-center">
				<slot name="title" />
			</div>
			<div
				v-if="!props.isLoading"
				class="text-xl text-theme-interactive flex flex-row gap-2"
			>
				<div class="w-6 h-6 mr-2 text-theme-interactive">
					<slot name="icon" />
				</div>
				<div class="numerical"><slot name="value" /></div>
				<div><slot name="unit" /></div>
				<Chip class="self-center"><slot name="chip" /></Chip>
			</div>
		</header>

		<ClientOnly>
			<div v-if="props.isLoading" class="w-full h-36 text-center">
				<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
			</div>
			<ChartLineArea
				v-else-if="
					props.chartType === 'area' && props.xValues && props.yValues.length
				"
				class="h-28 w-full"
				:x-values="props.xValues"
				:y-values-high="props.yValues[0] ?? undefined"
				:y-values-mid="props.yValues[1]"
				:y-values-low="props.yValues[2]"
				:begin-at-zero="props.beginAtZero"
				:label-formatter="props.labelFormatter"
				:bucket-width="props.bucketWidth"
			/>
			<ChartBar
				v-else-if="
					props.chartType === 'bar' && props.xValues && props.yValues.length
				"
				class="h-28 w-full"
				:x-values="props.xValues"
				:y-values="props.yValues[0]"
				:begin-at-zero="props.beginAtZero"
				:bucket-width="props.bucketWidth"
				:label-formatter="props.labelFormatter"
			/>
			<ChartLine
				v-else-if="props.xValues && props.yValues"
				class="h-28 w-full"
				:x-values="props.xValues"
				:begin-at-zero="props.beginAtZero"
				:y-values="props.yValues[0]"
				:bucket-width="props.bucketWidth"
				:label-formatter="props.labelFormatter"
			/>
		</ClientOnly>
	</MetricCard>
</template>
<script lang="ts" setup>
import ChartLine from '~/components/Charts/ChartLine.vue'
import ChartLineArea from '~/components/Charts/ChartLineArea.vue'
import Chip from '~/components/atoms/Chip.vue'
import MetricCard from '~/components/atoms/MetricCard.vue'
import ChartBar from '~/components/Charts/ChartBar.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import type { LabelFormatterFunc } from '~/components/Charts/ChartUtils'

type Props = {
	yValues: ((number | null)[] | undefined)[]
	xValues?: string[]
	chartType?: string
	bucketWidth?: string
	beginAtZero?: boolean
	isLoading?: boolean
	labelFormatter?: LabelFormatterFunc
}
const props = defineProps<Props>()
</script>
