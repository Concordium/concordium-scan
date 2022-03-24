<template>
	<span>
		<AccountLink :address="event.fromAccountAddressString" /> initiated a
		scheduled transfer to
		<AccountLink :address="event.toAccountAddressString" />
	</span>
</template>

<script setup lang="ts">
import type { TransferredWithSchedule } from '~/types/generated'

type Props = {
	event: TransferredWithSchedule
}

defineProps<Props>()
</script>
