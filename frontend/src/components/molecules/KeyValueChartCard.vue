<template>
	<div
		class="flex flex-row cardShadow rounded-2xl shadow-2xl my-4 relative overflow-hidden bg-theme-background-primary-elevated"
	>
		<div class="flex flex-col items-center w-full">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight"></slot>
			</div>

			<div class="text-sm text-theme-faded pt-4 w-72 text-center">
				<slot name="title"></slot>
			</div>

			<div class="text-xl text-theme-interactive flex flex-row gap-2">
				<div class="w-6 h-6 mr-2 text-theme-interactive">
					<slot name="icon"></slot>
				</div>
				<div class="numerical"><slot name="value"></slot></div>
				<div><slot name="unit"></slot></div>
				<Chip class="self-center"><slot name="chip"></slot></Chip>
			</div>
			<ClientOnly>
				<div v-if="props.chartType == 'area'" class="h-full w-full">
					<ChartLineArea
						v-if="props.xValues && props.yValues.length"
						class="h-28"
						:x-values="props.xValues"
						:y-values-high="props.yValues[0] ?? undefined"
						:y-values-mid="props.yValues[1]"
						:y-values-low="props.yValues[2]"
						:begin-at-zero="props.beginAtZero"
						:bucket-width="props.bucketWidth"
					></ChartLineArea>
				</div>
				<div v-if="props.chartType == 'bar'" class="h-full w-full">
					<ChartBar
						v-if="props.xValues && props.yValues.length"
						class="h-28"
						:x-values="props.xValues"
						:y-values="props.yValues[0]"
						:begin-at-zero="props.beginAtZero"
						:bucket-width="props.bucketWidth"
					></ChartBar>
				</div>
				<div v-else class="h-full w-full">
					<ChartLine
						v-if="props.xValues && props.yValues"
						class="h-28"
						:x-values="props.xValues"
						:begin-at-zero="props.beginAtZero"
						:y-values="props.yValues[0]"
						:bucket-width="props.bucketWidth"
					></ChartLine>
				</div>
			</ClientOnly>
		</div>
	</div>
</template>
<script lang="ts" setup>
import ChartLine from '~/components/Charts/ChartLine.vue'
import ChartLineArea from '~/components/Charts/ChartLineArea.vue'
import Chip from '~/components/atoms/Chip.vue'
import ChartBar from '~/components/Charts/ChartBar.vue'

type Props = {
	yValues: ((number | null)[] | undefined)[]
	xValues?: string[]
	chartType?: string
	bucketWidth?: string
	beginAtZero?: boolean
}
const props = defineProps<Props>()
</script>
