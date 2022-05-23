<template>
	<span>
		Delegation added for delegator
		<AccountLink :address="event.accountAddress.asString" /> (id
		{{ event.delegatorId }})</span
	>
</template>

<script setup lang="ts">
import type { DelegationAdded } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	event: DelegationAdded
}

defineProps<Props>()
</script>
