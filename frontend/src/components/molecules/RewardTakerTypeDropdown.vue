<template>
	<div class="bg-theme-background-primary-elevated pt-2 px-2 pb-1 rounded-xl">
		<FunnelIcon class="w-3 h-3" />
		<select
			class="form-select bg-theme-background-primary-elevated-nontrans"
			:value="refVal"
			@input="handleOnChange"
		>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === RewardTakerTypes.Total"
				:value="RewardTakerTypes.Total"
			>
				Total
			</option>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === RewardTakerTypes.Delegators"
				:value="RewardTakerTypes.Delegators"
			>
				Delegators
			</option>
			<option
				class="bg-theme-background-primary"
				:selected="refVal === RewardTakerTypes.Bakers"
				:value="RewardTakerTypes.Bakers"
			>
				Bakers
			</option>
		</select>
	</div>
</template>
<script lang="ts" setup>
import { toRef } from 'vue'

import FunnelIcon from '~/components/icons/FunnelIcon.vue'
import { RewardTakerTypes } from '~/types/rewardTakerTypes'
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
