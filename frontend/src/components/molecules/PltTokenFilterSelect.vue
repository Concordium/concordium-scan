<template>
	<div class="bg-theme-background-table pt-2 px-2 pb-1 rounded-xl">
		<FunnelIcon class="w-3 h-3" />
		<select
			class="form-select bg-theme-background-table"
			:value="refVal === null ? '' : String(refVal)"
			@input="handleOnChange"
		>
			<option class="bg-theme-background-table text-theme-white" value="">
				All
			</option>
			<option class="bg-theme-background-table text-theme-white" value="false">
				Active
			</option>
			<option class="bg-theme-background-table text-theme-white" value="true">
				Paused
			</option>
		</select>
	</div>
</template>
<script lang="ts" setup>
import { toRef } from 'vue'
import FunnelIcon from '~/components/icons/FunnelIcon.vue'
const emit = defineEmits(['update:modelValue'])

type Props = {
	modelValue: boolean | null
}

const props = defineProps<Props>()
const refVal = toRef(props, 'modelValue')

const handleOnChange = (event: Event) => {
	const target = event.target as HTMLSelectElement
	const value = target.value
	// Convert string value to boolean or null
	if (value === '' || value === 'null') {
		emit('update:modelValue', null)
	} else if (value === 'true') {
		emit('update:modelValue', true)
	} else if (value === 'false') {
		emit('update:modelValue', false)
	}
}
</script>
