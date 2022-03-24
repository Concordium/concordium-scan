<template>
	<span>
		Account created with address
		<AccountLink :address="event.accountAddressString" />
	</span>
</template>

<script setup lang="ts">
import type { AccountCreated } from '~/types/generated'

type Props = {
	event: AccountCreated
}

defineProps<Props>()
</script>
