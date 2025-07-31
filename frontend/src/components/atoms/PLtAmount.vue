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

const numberFormatter = (num: number, decimals: number) => {
	if (isNaN(num)) return { formatedNum: '0', suffix: '' }

	const units = [
		{ threshold: 1e12, suffix: 'T' },
		{ threshold: 1e9, suffix: 'B' },
		{ threshold: 1e6, suffix: 'M' },
		{ threshold: 1e3, suffix: 'K' },
	]

	const unit = units.find(u => num >= u.threshold)
	return unit
		? {
				formatedNum: (num / unit.threshold).toFixed(decimals),
				suffix: unit.suffix,
		  }
		: { formatedNum: num.toFixed(decimals), suffix: '' }
}

const amounts: ComputedRef<[string, string]> = computed(() => {
	const valueStr = props.value.toString()

	let significantDigits: string
	let nonSignificantDigits: string

	if (props.decimals === 0) {
		// No decimal places - everything is significant
		significantDigits = valueStr
		nonSignificantDigits = ''
	} else {
		// Has decimal places
		const totalLength = valueStr.length

		if (totalLength <= props.decimals) {
			// If value has fewer digits than decimals, pad with leading zeros
			significantDigits = '0'
			nonSignificantDigits = valueStr.padStart(props.decimals, '0')
		} else {
			// Split normally: last 'decimals' digits are fractional
			significantDigits = valueStr.slice(0, totalLength - props.decimals)
			nonSignificantDigits = valueStr.slice(totalLength - props.decimals)
		}
	}

	const numericValue =
		Number(significantDigits) +
		Number(nonSignificantDigits || '0') / 10 ** props.decimals

	if (props.formatNumber) {
		const { formatedNum, suffix } = numberFormatter(
			numericValue,
			props.decimals
		)
		// Trim trailing zeros and format the number
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
