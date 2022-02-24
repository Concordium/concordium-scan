<template>
	<div>
		<FunnelIcon class="w-2 h-2" /><select
			class="form-select bg-theme-background-primary"
			:value="refVal"
			@input="emit('update:modelValue', $event.target.value)"
		>
			<option
				:selected="refVal === MetricsPeriod.LastHour"
				:value="MetricsPeriod.LastHour"
			>
				1 hour
			</option>
			<option
				:selected="refVal === MetricsPeriod.Last24Hours"
				:value="MetricsPeriod.Last24Hours"
			>
				24 hour
			</option>
			<option
				:selected="refVal === MetricsPeriod.Last7Days"
				:value="MetricsPeriod.Last7Days"
			>
				7 days
			</option>
			<option
				:selected="refVal === MetricsPeriod.Last30Days"
				:value="MetricsPeriod.Last30Days"
			>
				30 days
			</option>
			<option
				:selected="refVal === MetricsPeriod.LastYear"
				:value="MetricsPeriod.LastYear"
			>
				1 year
			</option>
		</select>
	</div>
</template>
<script lang="ts" setup>
import { MetricsPeriod } from '~/types/generated'
import FunnelIcon from '~/components/icons/FunnelIcon.vue'
const emit = defineEmits(['update:modelValue'])

type Props = {
	modelValue: string
}
const props = defineProps<Props>()
const refVal = toRef(props, 'modelValue')
</script>
