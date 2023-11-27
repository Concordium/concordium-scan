<template>
	<Tooltip
		v-if="
			props.nextPaydayCommission &&
			props.nextPaydayCommission !== props.currentPaydayCommission
		"
		:text="`Rates will change to ${formatPercentage(
			props.nextPaydayCommission
		)}% on the next pay day.`"
	>
		<span class="numerical change">
			{{
				props.currentPaydayCommission
					? formatPercentage(props.currentPaydayCommission)
					: 'unknown'
			}}%
			<ArrowUpIcon
				v-if="
					!props.currentPaydayCommission ||
					props.nextPaydayCommission > props.currentPaydayCommission
				"
				class="h-4 align-middle"
			/>
			<ArrowDownIcon v-else class="h-4 align-middle" />
			<WarningIcon class="h-4 align-middle" />
		</span>
	</Tooltip>
	<span v-else class="numerical">
		{{
			props.currentPaydayCommission
				? formatPercentage(props.currentPaydayCommission)
				: 'unknown'
		}}%
	</span>
</template>
<script lang="ts" setup>
import WarningIcon from '../icons/WarningIcon.vue'
import Tooltip from '../atoms/Tooltip.vue'
import ArrowUpIcon from '../icons/ArrowUpIcon.vue'
import ArrowDownIcon from '../icons/ArrowDownIcon.vue'
import { formatPercentage } from '~/utils/format'

type Props = {
	currentPaydayCommission: number | null
	nextPaydayCommission: number | null
}
const props = defineProps<Props>()
</script>
<style>
.change {
	color: #ffc600;
}
</style>
