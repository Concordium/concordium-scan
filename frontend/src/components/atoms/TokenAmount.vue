<template>
	<span
		class="inline-block"
		:class="removeTopPadding ? '' : 'pt-1'"
		data-testid="amount"
	>
		{{ symbol }}
		<span class="numerical"> {{ amounts[0] }}</span>
		<span v-if="amounts[1]" class="numerical text-sm opacity-50"
			>.{{ amounts[1] }}</span
		>
	</span>
</template>

<script lang="ts" setup>
import { computed, type ComputedRef } from 'vue'

const defaultFractionDigits = 0

type Props = {
	amount: string
	removeTopPadding?: boolean
	fractionDigits?: number | undefined
	symbol?: string | undefined
}

const props = defineProps<Props>()

const fractionDigits = computed(() =>
	props.fractionDigits === undefined
		? defaultFractionDigits
		: props.fractionDigits
)

const amounts: ComputedRef<[string, string]> = computed(() => {
	const amount = props.amount.padStart(fractionDigits.value, '0')
	const significantDigits = amount.substring(
		0,
		amount.length - fractionDigits.value
	)
	const nonSignificantDigits = amount.substring(
		amount.length - fractionDigits.value,
		amount.length
	)

	const number = new Intl.NumberFormat(undefined, {}).format(
		BigInt(significantDigits)
	)
	return [number, nonSignificantDigits]
})
</script>
