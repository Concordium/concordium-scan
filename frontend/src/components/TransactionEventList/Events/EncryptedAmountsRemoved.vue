<template>
	<span>
		Removed an amount from the encrypted balance of account
		<AccountLink :address="event.accountAddressString" />
	</span>
</template>

<script setup lang="ts">
import type { EncryptedAmountsRemoved } from '~/types/generated'

type Props = {
	event: EncryptedAmountsRemoved
}

defineProps<Props>()
</script>
