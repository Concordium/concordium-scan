<template>
	<span>
		<AccountLink :address="reason.accountAddress.asString" /> is not a delegator
	</span>
</template>

<script setup lang="ts">
import type { NotADelegator } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	reason: NotADelegator
}

defineProps<Props>()
</script>
