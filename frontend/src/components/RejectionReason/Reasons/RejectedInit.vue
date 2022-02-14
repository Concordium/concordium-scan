<template>
	<span>
		Rejected due to contract logic in init function of a contract <br />
		Error code: <span class="numerical">{{ reason.rejectReason }}</span>
	</span>
</template>

<script setup lang="ts">
import type { RejectedInit } from '~/types/generated'

type Props = {
	reason: RejectedInit
}

defineProps<Props>()
</script>
