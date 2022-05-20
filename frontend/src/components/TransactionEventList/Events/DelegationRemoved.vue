<template>
	<span>
		Delegation removed for
		<AccountLink :address="event.accountAddress.asString" /> (delegator
		{{ event.delegatorId }})</span
	>
</template>

<script setup lang="ts">
import type { DelegationRemoved } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	event: DelegationRemoved
}

defineProps<Props>()
</script>
