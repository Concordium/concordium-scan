<template>
	<div
		class="flex flex-row cardShadow rounded-2xl shadow-2xl m-4 relative overflow-hidden bg-theme-background-primary-elevated"
	>
		<div class="flex flex-col items-center w-full">
			<div class="absolute top-4 right-4 text-xs">
				<slot name="topRight"></slot>
			</div>

			<div class="text-xl pt-4 w-72 text-center">
				<slot name="title"></slot>
			</div>
			<div class="flex flex-row">
				<div class="w-4 h-4 mr-2 text-theme-interactive">
					<slot name="icon"></slot>
				</div>
				<div class="text-sm text-theme-interactive">
					<slot name="value"></slot>
					<slot name="unit"></slot>
				</div>
			</div>
			<div v-if="props.chartType == 'area'" class="h-full w-full">
				<ChartLineArea
					v-if="props.xValues && props.yValues"
					:x-values="props.xValues"
					:y-values-high="props.yValues[0]"
					:y-values-mid="props.yValues[1]"
					:y-values-low="props.yValues[2]"
				></ChartLineArea>
			</div>
			<div v-else class="h-full w-full">
				<ChartLine
					v-if="props.xValues && props.yValues"
					:x-values="props.xValues"
					:y-values="props.yValues"
				></ChartLine>
			</div>
		</div>
	</div>
</template>
<script lang="ts" setup>
import ChartLine from '~/components/Charts/ChartLine.vue'
import ChartLineArea from '~/components/Charts/ChartLineArea.vue'

type Props = {
	xValues?: unknown[]
	yValues?: unknown[]
	chartType?: string
}
const props = defineProps<Props>()
</script>
