<template>
	<span
		class="inline-block"
		:class="removeTopPadding ? '' : 'pt-1'"
		data-testid="amount"
	>
		{{ symbol }}
		<span class="numerical"> {{ formatNumber(amounts[0]) }}</span>

		<span class="numerical text-sm opacity-50">{{ amounts[1] }}</span>
	</span>
</template>

<script lang="ts" setup>
import { computed, type ComputedRef } from 'vue'
import { formatNumber } from '~/utils/format'

const defaultFractionDigits = 0

type Props = {
	amount: number
	removeTopPadding?: boolean
	fractionDigits?: number
	symbol?: string
}

const props = defineProps<Props>()

const fractionDigits = computed(() =>
	props.fractionDigits === undefined
		? defaultFractionDigits
		: props.fractionDigits
)

const amounts: ComputedRef<[number, string]> = computed(() => {
	const unit = Math.floor(props.amount)

	const subUnit = new Intl.NumberFormat(undefined, {
		minimumFractionDigits: fractionDigits.value,
		maximumFractionDigits: fractionDigits.value,
	})
		.format(props.amount - unit)
		.slice(1)

	return [unit, subUnit]
})
</script>
