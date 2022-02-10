<template>
	<span>
		<Hash :hash="event.fromAccountAddress" /> initiated a scheduled transfer to
		<Hash :hash="event.toAccountAddress" />
	</span>
</template>

<script setup lang="ts">
import type { TransferredWithSchedule } from '~/types/generated'

type Props = {
	event: TransferredWithSchedule
}

defineProps<Props>()
</script>
