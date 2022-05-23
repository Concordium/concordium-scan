<template>
	<span>
		Delegation removed for delegator
		<AccountLink :address="event.accountAddress.asString" /> (id
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
