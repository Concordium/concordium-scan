<template>
	<span>
		Account <AccountLink :address="event.accountAddress" /> transferred
		{{ convertMicroCcdToCcd(event.amount) }}Ͼ from public to encrypted balance
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import type { EncryptedSelfAmountAdded } from '~/types/generated'

type Props = {
	event: EncryptedSelfAmountAdded
}

defineProps<Props>()
</script>
