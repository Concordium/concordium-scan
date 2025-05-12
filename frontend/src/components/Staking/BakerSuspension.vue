<template>
	<Tooltip
		v-if="
			!Number.isInteger(props.selfSuspended) &&
			!Number.isInteger(props.inactiveSuspended) &&
			!Number.isInteger(props.primedForSuspension)
		"
		:text="`Validator is active.`"
	>
		<span class="numerical change" style="color: hsl(var(--color-interactive))">
			Active
		</span>
	</Tooltip>
	<Tooltip
		v-else-if="
			Number.isInteger(props.primedForSuspension) &&
			!Number.isInteger(props.selfSuspended) &&
			!Number.isInteger(props.inactiveSuspended)
		"
		:text="`Validator will be suspended on the next pay day.`"
	>
		<span class="numerical change" style="color: #ffc600">
			Primed
			<WarningIcon class="h-4 align-middle" />
		</span>
	</Tooltip>
	<Tooltip
		v-else-if="Number.isInteger(props.inactiveSuspended)"
		:text="`Validator is suspended due to inactivity.`"
	>
		<span class="numerical change" style="color: #ffc600">
			Suspended
			<WarningIcon class="h-4 align-middle" />
		</span>
	</Tooltip>
	<Tooltip
		v-else-if="Number.isInteger(props.selfSuspended)"
		:text="`Validator is suspended because the validator sent a self-suspending transaction.`"
	>
		<span class="numerical change" style="color: #ffc600">
			Suspended
			<WarningIcon class="h-4 align-middle" />
		</span>
	</Tooltip>
</template>
<script lang="ts" setup>
import WarningIcon from '../icons/WarningIcon.vue'
import Tooltip from '../atoms/Tooltip.vue'

type Props = {
	selfSuspended: number | null | undefined
	inactiveSuspended: number | null | undefined
	primedForSuspension: number | null | undefined
}
const props = defineProps<Props>()
</script>
