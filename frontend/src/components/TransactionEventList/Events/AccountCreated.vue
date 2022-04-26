<template>
	<span>
		Account created with address
		<AccountLink :address="event.accountAddress.asString" />
	</span>
</template>

<script setup lang="ts">
import type { AccountCreated } from '~/types/generated'

type Props = {
	event: AccountCreated
}

defineProps<Props>()
</script>
