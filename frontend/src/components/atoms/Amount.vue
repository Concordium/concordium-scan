<template>
	<span class="inline-block pt-1" data-testid="amount">
		{{ symbol }}
		<span class="numerical"> {{ formatNumber(amounts[0]) }}</span>

		<span class="numerical text-sm opacity-50">{{ amounts[1] }}</span>
	</span>
</template>

<script lang="ts" setup>
import { computed, type ComputedRef } from 'vue'
import { formatNumber } from '~/utils/format'

type Props = {
	amount: number
	showSymbol?: boolean
}

const props = defineProps<Props>()

const symbol = computed(() => (props.showSymbol ? 'Ͼ' : undefined))

const amounts: ComputedRef<[number, string]> = computed(() => {
	const ccdAmount = props.amount / 1_000_000
	const unit = Math.floor(ccdAmount)

	const subUnit = new Intl.NumberFormat(undefined, {
		minimumFractionDigits: 6,
		maximumFractionDigits: 6,
	})
		.format(ccdAmount - unit)
		.slice(1)

	return [unit, subUnit]
})
</script>
