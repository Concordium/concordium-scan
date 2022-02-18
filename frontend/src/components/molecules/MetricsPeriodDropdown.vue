<template>
	<div>
		<FunnelIcon class="w-2 h-2" /><select
			class="form-select bg-theme-background-primary"
			:value="props.modelValue"
			@input="emit('update:modelValue', $event.target.value)"
		>
			<option :value="MetricsPeriod.LastHour">1 hour</option>
			<option :value="MetricsPeriod.Last24Hours">24 hour</option>
			<option :value="MetricsPeriod.Last7Days">7 days</option>
			<option :value="MetricsPeriod.Last30Days">30 days</option>
			<option :value="MetricsPeriod.LastYear">1 year</option>
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
</script>
