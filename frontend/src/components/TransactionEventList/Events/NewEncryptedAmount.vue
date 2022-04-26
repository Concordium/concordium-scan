<template>
	<span>
		Added an amount to the encrypted balance of account
		<AccountLink :address="event.accountAddress.asString" />
	</span>
</template>

<script setup lang="ts">
import type { NewEncryptedAmount } from '~/types/generated'

type Props = {
	event: NewEncryptedAmount
}

defineProps<Props>()
</script>
