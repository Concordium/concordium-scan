<template>
	<div
		class="flex flex-row cardShadow rounded-2xl shadow-2xl m-4 relative overflow-hidden bg-theme-background-primary-elevated"
	>
		<div class="flex flex-col flex-shrink-0 items-center w-full">
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
			<ChartLine
				class="h-full w-full"
				:x-values="props.xValues"
				:y-values="props.yValues"
			></ChartLine>
		</div>
	</div>
</template>
<script lang="ts" setup>
import ChartLine from '~/components/Charts/ChartLine.vue'
type Props = {
	xValues: unknown[]
	yValues: unknown[]
}
const props = defineProps<Props>()
</script>
