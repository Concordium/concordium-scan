<template>
	<span>
		Rejected due to contract logic in receive function '{{
			reason.receiveName
		}}' of contract <Contract :address="reason.contractAddress" /> <br />
		Error code: <span class="numerical">{{ reason.rejectReason }}</span>
	</span>
</template>

<script setup lang="ts">
import Contract from '~/components/molecules/Contract.vue'
import type { RejectedReceive } from '~/types/generated'

type Props = {
	reason: RejectedReceive
}

defineProps<Props>()
</script>
