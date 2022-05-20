<template>
	<span>
		<AccountLink :address="event.accountAddress.asString" /> (delegator
		{{ event.delegatorId }}) increased stake to
		{{ convertMicroCcdToCcd(event.newStakedAmount) }}Ͼ</span
	>
</template>

<script setup lang="ts">
import type { DelegationStakeIncreased } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import { convertMicroCcdToCcd } from '~/utils/format'

type Props = {
	event: DelegationStakeIncreased
}

defineProps<Props>()
</script>
