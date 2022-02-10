<template>
	<span>
		Account <Hash :hash="event.accountAddress" /> transferred
		{{ convertMicroCcdToCcd(event.amount) }}Ͼ from encrypted to public balance
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import Hash from '~/components/molecules/Hash.vue'
import type { AmountAddedByDecryption } from '~/types/generated'

type Props = {
	event: AmountAddedByDecryption
}

defineProps<Props>()
</script>
