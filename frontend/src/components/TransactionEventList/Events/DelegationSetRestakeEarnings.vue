<template>
	<span>
		<AccountLink :address="event.accountAddress.asString" /> (delegator
		{{ event.delegatorId }}) modified to
		{{
			event.restakeEarnings
				? ' restake earnings'
				: ' no longer restake earnings'
		}}</span
	>
</template>

<script setup lang="ts">
import type { DelegationSetRestakeEarnings } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	event: DelegationSetRestakeEarnings
}

defineProps<Props>()
</script>
