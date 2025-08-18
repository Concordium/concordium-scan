<template>
	<span class="inline-block" data-testid="amount">
		<span class="numerical">{{ amounts[0] }}</span>
		<span v-if="amounts[1]" class="numerical text-sm opacity-50"
			>.{{ amounts[1] }}</span
		>
		<span class="text-sm opacity-50">{{ ' %' }}</span>
	</span>
</template>

<script lang="ts" setup>
import { computed, type ComputedRef } from 'vue'

type Props = {
	value: string
}

const props = defineProps<Props>()

const amounts: ComputedRef<[string, string]> = computed(() => {
	const valueStr = props.value.toString()
	const significantDigits = valueStr.split('.')[0] || '0'
	const nonSignificantDigits = valueStr.split('.')[1] || '00'

	return [significantDigits, nonSignificantDigits]
})
</script>
