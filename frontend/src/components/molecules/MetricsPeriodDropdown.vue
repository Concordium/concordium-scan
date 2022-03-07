<template>
	<div class="bg-theme-background-primary-elevated pt-2 px-2 pb-1 rounded-xl">
		<FunnelIcon class="w-3 h-3" /><select
			class="form-select bg-theme-background-primary-elevated-nontrans"
			:value="refVal"
			@input="emit('update:modelValue', $event.target.value)"
		>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === MetricsPeriod.LastHour"
				:value="MetricsPeriod.LastHour"
			>
				1 hour
			</option>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === MetricsPeriod.Last24Hours"
				:value="MetricsPeriod.Last24Hours"
			>
				24 hour
			</option>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === MetricsPeriod.Last7Days"
				:value="MetricsPeriod.Last7Days"
			>
				7 days
			</option>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === MetricsPeriod.Last30Days"
				:value="MetricsPeriod.Last30Days"
			>
				30 days
			</option>
			<option
				class="bg-theme-background-primary"
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
