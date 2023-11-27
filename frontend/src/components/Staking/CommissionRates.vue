<template>
	<Tooltip
		v-if="
			props.currentPaydayCommission &&
			props.nextPaydayCommission !== props.currentPaydayCommission
		"
		:text="`Rates will change to ${
			props.nextPaydayCommission
				? formatPercentage(props.nextPaydayCommission)
				: 'unknown'
		}% on the next pay day.`"
	>
		<span class="numerical change">
			{{
				props.nextPaydayCommission
					? formatPercentage(props.currentPaydayCommission)
					: 'unknown'
			}}%
			<WarningIcon class="h-4 inline align-text-top" />
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
