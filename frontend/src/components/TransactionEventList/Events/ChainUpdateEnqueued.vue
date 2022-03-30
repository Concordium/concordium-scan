<template>
	<span>
		Chain update enqueued effective at
		{{ formatTimestamp(event.effectiveTime) }}
		<span
			v-if="event.payload.__typename === 'MicroCcdPerEuroChainUpdatePayload'"
			class="text-violet-500 text-theme-faded"
		>
			<br />
			The CCD/EUR exchange rate was updated to
			{{
				convertMicroCcdToCcd(
					event.payload.exchangeRate.numerator /
						event.payload.exchangeRate.denominator
				)
			}}
		</span>
	</span>
</template>

<script setup lang="ts">
import { formatTimestamp, convertMicroCcdToCcd } from '~/utils/format'
import type { ChainUpdateEnqueued } from '~/types/generated'

type Props = {
	event: ChainUpdateEnqueued
}

defineProps<Props>()
</script>
