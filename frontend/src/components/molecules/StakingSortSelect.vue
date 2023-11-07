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
				:selected="refVal === BakerSort.TotalStakedAmountDesc"
				:value="BakerSort.TotalStakedAmountDesc"
			>
				Highest total stake
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.DelegatorCountDesc"
				:value="BakerSort.DelegatorCountDesc"
			>
				Most delegators
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.BakerApy30DaysDesc"
				:value="BakerSort.BakerApy30DaysDesc"
			>
				Highest 30-day validator APY
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.DelegatorApy30DaysDesc"
				:value="BakerSort.DelegatorApy30DaysDesc"
			>
				Highest 30-day delegators APY
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.BakerIdDesc"
				:value="BakerSort.BakerIdDesc"
			>
				Highest validator ID
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.BakerIdAsc"
				:value="BakerSort.BakerIdAsc"
			>
				Lowest validator ID
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.ValidationCommissionsDesc"
				:value="BakerSort.ValidationCommissionsDesc"
			>
				Highest validation commission
			</option>
			<option
				class="bg-theme-background-table text-theme-white"
				:selected="refVal === BakerSort.ValidationCommissionsAsc"
				:value="BakerSort.ValidationCommissionsAsc"
			>
				Lowest validation commission
			</option>
		</select>
	</div>
</template>
<script lang="ts" setup>
import { toRef } from 'vue'
import { BakerSort } from '~/types/generated'
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
