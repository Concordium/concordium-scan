<template>
	<span>
		<AccountLink :address="event.fromAccountAddress" /> initiated a scheduled
		transfer to
		<AccountLink :address="event.toAccountAddress" />
	</span>
</template>

<script setup lang="ts">
import type { TransferredWithSchedule } from '~/types/generated'

type Props = {
	event: TransferredWithSchedule
}

defineProps<Props>()
</script>
