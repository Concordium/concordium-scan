<template>
	<div class="bg-theme-background-table pt-2 px-2 pb-1 rounded-xl">
		<SortIcon class="w-3 h-3" />
		<select
			class="form-select bg-theme-background-table"
			:value="refVal"
			@input="handleOnChange"
		>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === PltTokenSort.AgeDesc"
				:value="PltTokenSort.AgeDesc"
			>
				Newest
			</option>

			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === PltTokenSort.SupplyDesc"
				:value="PltTokenSort.SupplyDesc"
			>
				Supply
			</option>
		</select>
	</div>
</template>
<script lang="ts" setup>
import { toRef } from 'vue'
import { PltTokenSort } from '~/types/generated'
import SortIcon from '~/components/icons/SortIcon.vue'
const emit = defineEmits(['update:modelValue'])

type Props = {
	modelValue: string
}

const props = defineProps<Props>()
const refVal = toRef(props, 'modelValue')

const handleOnChange = (event: Event) => {
	// compiler does not know if `EventTarget` has a `value` (for example if it is a div)
	const target = event.target as HTMLSelectElement
	emit('update:modelValue', target.value)
}
</script>
