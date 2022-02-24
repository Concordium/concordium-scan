<template>
	<span>
		Account <AccountLink :address="event.accountAddress" /> transferred
		{{ convertMicroCcdToCcd(event.amount) }}Ͼ from encrypted to public balance
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import type { AmountAddedByDecryption } from '~/types/generated'

type Props = {
	event: AmountAddedByDecryption
}

defineProps<Props>()
</script>
