<template>
	<span class="inline-block" data-testid="amount">
		<span class="numerical">{{ amounts[0] }}</span>
		<span v-if="amounts[1]" class="numerical text-sm opacity-50"
			>.{{ amounts[1] }}</span
		>
	</span>
</template>

<script lang="ts" setup>
import { computed, type ComputedRef } from 'vue'

type Props = {
	value: string
	decimals: number
	formatNumber?: boolean
}

const props = defineProps<Props>()

const numberFormatter = (significantBigInt: bigint, decimals: number) => {
	const units = [
		{ threshold: 1000000000000n, suffix: 'T' },
		{ threshold: 1000000000n, suffix: 'B' },
		{ threshold: 1000000n, suffix: 'M' },
		{ threshold: 1000n, suffix: 'K' },
	]

	const unit = units.find(u => significantBigInt >= u.threshold)
	if (unit) {
		const totalValue = Number(significantBigInt) / Number(unit.threshold)
		return {
			formatedNum: totalValue.toFixed(decimals),
			suffix: unit.suffix,
		}
	}

	const totalValue = Number(significantBigInt) / 10 ** decimals
	return {
		formatedNum: totalValue.toFixed(decimals),
		suffix: '',
	}
}

const amounts: ComputedRef<[string, string]> = computed(() => {
	const valueStr = props.value.toString()
	const totalLength = valueStr.length

	// Calculate significant and non-significant digits
	let significantDigits: string
	let nonSignificantDigits: string

	if (props.decimals === 0) {
		significantDigits = valueStr
		nonSignificantDigits = ''
	} else if (totalLength <= props.decimals) {
		significantDigits = '0'
		nonSignificantDigits = valueStr.padStart(props.decimals, '0')
	} else {
		significantDigits = valueStr.slice(0, totalLength - props.decimals)
		nonSignificantDigits = valueStr.slice(totalLength - props.decimals)
	}

	if (props.formatNumber) {
		const rawValue = BigInt(props.value)
		const { formatedNum, suffix } = numberFormatter(rawValue, props.decimals)

		const trimmed = formatedNum.replace(/\.?0+$/, '')
		return trimmed === '0'
			? ['0' + formatedNum.replace(trimmed, '') + suffix, '']
			: [trimmed + formatedNum.replace(trimmed, '') + suffix, '']
	}

	const formattedInteger = new Intl.NumberFormat().format(
		BigInt(significantDigits)
	)
	const trimmedDecimals =
		nonSignificantDigits.replace(/0+$/, '') || (props.decimals > 0 ? '0' : '')

	return [formattedInteger, trimmedDecimals]
})
</script>
