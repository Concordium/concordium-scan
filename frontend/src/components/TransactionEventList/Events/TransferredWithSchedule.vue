<template>
	<span>
		Transferred a total of
		<span class="numerical">{{ convertMicroCcdToCcd(event.totalAmount) }}</span
		>Ͼ from account <AccountLink :address="event.fromAccountAddressString" /> to
		account <AccountLink :address="event.toAccountAddressString" /> with a
		release schedule
	</span>
</template>
<script setup lang="ts">
import type { TransferredWithSchedule } from '~/types/generated'
import { convertMicroCcdToCcd } from '~/utils/format'
type Props = {
	event: TransferredWithSchedule
}

defineProps<Props>()
</script>
