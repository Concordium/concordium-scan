<template>
	<span
		class="inline-block"
		data-testid="amount"
		:title="formatBigIntWithDecimals(value, decimals)"
	>
		<span class="numerical">{{ amounts[0] }}</span>
		<span v-if="amounts[1]" class="numerical text opacity-50 mr-1"
			>.{{ amounts[1] }}</span
		>
	</span>
</template>

<script lang="ts" setup>
import { computed, type ComputedRef } from 'vue'

/**
 * Props for the PltAmount component
 */
type Props = {
	/** The raw token amount as a string (e.g., "1000000" for 1.000000 tokens with 6 decimals) */
	value: string
	/** The number of decimal places for the token (used to convert raw value to human-readable format) */
	decimals: number
	/**
	 * Whether to format large numbers with suffixes (K, M, B, T) for better readability
	 * @default false
	 */
	formatNumber?: boolean
	/**
	 * When formatNumber is true, this overrides the number of decimal places shown in the formatted output
	 * Useful for consistent display formatting regardless of token's native decimal places
	 * @example With fixedDecimals=2, "1234567" becomes "1.23M" instead of "1.234567M"
	 */
	fixedDecimals?: number
}

const props = defineProps<Props>()

/**
 * Formats a BigInt value with decimal places for display in tooltips
 * Preserves precision for large u64 values
 * @param value - The raw token amount as a string
 * @param decimals - Number of decimal places
 * @returns Formatted string with proper decimal representation
 */
const formatBigIntWithDecimals = (
	value: string,
	decimals: number,
	fixedAfterDecimals: number = 18
): string => {
	const bigIntValue = BigInt(value)

	// Early return for zero decimals
	if (decimals === 0) return bigIntValue.toString()

	const divisor = BigInt(10 ** decimals)
	const integerPart = bigIntValue / divisor
	const remainder = bigIntValue % divisor

	// Early return if no decimal part
	if (remainder === 0n) return integerPart.toString()

	// Convert remainder to decimal string with proper padding
	let decimalPart = remainder.toString().padStart(decimals, '0')

	// Limit decimal places and remove trailing zeros efficiently
	if (decimalPart.length > fixedAfterDecimals) {
		decimalPart = decimalPart.slice(0, fixedAfterDecimals)
	}
	decimalPart = decimalPart.replace(/0+$/, '')

	return `${integerPart}.${decimalPart}`
}

/**
 * Formats large numbers with appropriate suffixes (K, M, B, T)
 * @param significantBigInt - The significant part of the number as BigInt
 * @param decimals - Number of decimal places to show in the formatted output
 * @returns Object containing the formatted number string and suffix
 * @example
 * numberFormatter(BigInt(1234), 2) // returns { formatedNum: "1.23", suffix: "K" }
 * numberFormatter(BigInt(999), 2)  // returns { formatedNum: "999.00", suffix: "" }
 */
const numberFormatter = (significantBigInt: bigint, decimals: number) => {
	const units = [
		{ threshold: BigInt(1000000000000), suffix: 'T' },
		{ threshold: BigInt(1000000000), suffix: 'B' },
		{ threshold: BigInt(1000000), suffix: 'M' },
		{ threshold: BigInt(1000), suffix: 'K' },
	]

	const unit = units.find(u => significantBigInt >= u.threshold)

	if (unit) {
		const totalValue = Number(significantBigInt) / Number(unit.threshold)
		return {
			formatedNum: totalValue.toFixed(decimals),
			suffix: unit.suffix,
		}
	}

	const totalValue = Number(significantBigInt)
	return {
		formatedNum: totalValue.toFixed(decimals),
		suffix: '',
	}
}

/**
 * Computed property that processes the raw token value into display format
 * @returns A tuple [mainPart, decimalPart] where:
 *   - mainPart: The integer part with thousands separators or formatted with suffix
 *   - decimalPart: The decimal part (empty string if no decimals or when using formatNumber)
 * @example
 * // For value="1234567", decimals=6, formatNumber=false:
 * // returns ["1", "234567"] (displays as "1.234567")
 *
 * // For value="1234567000", decimals=6, formatNumber=true, fixedDecimals=2:
 * // returns ["1.23K", ""] (displays as "1.23K")
 */
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
		const { formatedNum, suffix } = numberFormatter(
			BigInt(significantDigits),
			props.fixedDecimals ? props.fixedDecimals : props.decimals
		)

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
