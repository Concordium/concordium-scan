<template>
	<div
		class="flex flex-row cardShadow rounded-2xl shadow-2xl m-4 relative overflow-hidden bg-theme-background-primary-elevated"
	>
		<div class="flex flex-col flex-shrink-0 items-center">
			<div class="text-xl pt-4 w-72 text-center">
				<slot name="title"></slot>
			</div>
			<div class="flex flex-row">
				<BlockIcon v-if="props.unitIconName === 'block'" class="w-4 h-4 mr-2" />
				<TransactionIcon
					v-if="props.unitIconName === 'transaction'"
					class="w-4 h-4 mr-2"
				/>
				<div class="text-sm text-theme-interactive">
					<slot name="value"></slot>
					<slot name="unit"></slot>
				</div>
			</div>
			<ChartLine
				v-if="isMounted"
				class="h-full"
				:x-values="props.xValues"
				:y-values="props.yValues"
				:chart-height="100"
				:chart-width="286"
			></ChartLine>
		</div>
	</div>
</template>
<script lang="ts" setup>
import { onMounted } from 'vue'
import ChartLine from '~/components/Charts/ChartLine.vue'
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import BlockIcon from '~/components/icons/BlockIcon.vue'
type Props = {
	xValues: unknown[]
	yValues: unknown[]
	unitIconName?: string
}
const props = defineProps<Props>()
const isMounted = ref(false)
onMounted(() => {
	isMounted.value = true
})
onUnmounted(() => {
	isMounted.value = false
})
</script>
