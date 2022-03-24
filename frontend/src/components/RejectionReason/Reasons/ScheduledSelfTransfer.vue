<template>
	<span>
		Scheduled transfer not allowed to the same account (<Hash
			:hash="reason.accountAddressString"
		/>)
	</span>
</template>

<script setup lang="ts">
import Hash from '~/components/molecules/Hash.vue'
import type { ScheduledSelfTransfer } from '~/types/generated'

type Props = {
	reason: ScheduledSelfTransfer
}

defineProps<Props>()
</script>
