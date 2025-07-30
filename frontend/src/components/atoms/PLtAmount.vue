<template>
	<span class="inline-block" data-testid="amount">
		<!-- {{ symbol }} -->
		<span class="numerical"> {{ formatNumber(amount[0]) }}</span>

		<span class="numerical text-sm opacity-50">{{ amount[1] }}</span>
	</span>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import { formatNumber } from '~/utils/format'

type Props = {
	value: number
	decimals: number
}

const props = defineProps<Props>()

const amount = computed((): [number, string] => {
	const value = props.value / 10 ** props.decimals
	const decimals =
		value > 1
			? '.' + '0'.repeat(props.decimals)
			: '.' + (value.toString().split('.')[1] || '0'.repeat(props.decimals))

	return [value < 1 ? 0 : value, decimals]
})
</script>
