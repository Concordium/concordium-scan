<template>
	<span>
		<AccountLink :address="event.accountAddress.asString" /> (delegator
		{{ event.delegatorId }}) decreased stake to
		{{ convertMicroCcdToCcd(event.newStakedAmount) }}Ͼ</span
	>
</template>

<script setup lang="ts">
import type { DelegationStakeDecreased } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import { convertMicroCcdToCcd } from '~/utils/format'

type Props = {
	event: DelegationStakeDecreased
}

defineProps<Props>()
</script>
