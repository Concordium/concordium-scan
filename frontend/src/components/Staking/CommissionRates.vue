<template>
	<Tooltip
		v-if="
			props.currentPaydayCommission === undefined ||
			props.currentPaydayCommission === null
		"
		:text="`Validator is new and commissions rates will be active on next payday.`"
	>
		<span class="numerical change">
			{{ `${formatPercentage(props.nextPaydayCommission)}%` }}
			<WarningIcon class="h-4 align-middle" />
		</span>
	</Tooltip>
	<Tooltip
		v-else-if="props.nextPaydayCommission !== props.currentPaydayCommission"
		:text="`Rates will change to ${formatPercentage(
			props.nextPaydayCommission
		)}% on the next payday.`"
	>
		<span class="numerical change">
			{{ `${formatPercentage(props.currentPaydayCommission)}%` }}
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
		{{ `${formatPercentage(props.currentPaydayCommission)}%` }}
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
	nextPaydayCommission: number
}
const props = defineProps<Props>()
</script>
<style>
.change {
	color: #ffc600;
}
</style>
