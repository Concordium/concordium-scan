<template>
	<div
		class="flex flex-row border border-solid border-blue-300 cardShadow rounded-2xl shadow-2xl p-4 m-4"
	>
		<div class="flex flex-col pr-4">
			<DetailsCard>
				<template #title><slot name="title"></slot></template>
				<template #default><slot name="value"></slot></template>
			</DetailsCard>
		</div>
		<ChartLine
			class="h-full"
			:x-values="props.xValues"
			:y-values="props.yValues"
		></ChartLine>
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
