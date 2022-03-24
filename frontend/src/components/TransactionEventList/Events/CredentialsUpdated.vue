<template>
	<span>
		Account <AccountLink :address="event.accountAddressString" /> updated the
		credentials
	</span>
</template>

<script setup lang="ts">
import type { CredentialsUpdated } from '~/types/generated'

type Props = {
	event: CredentialsUpdated
}

defineProps<Props>()
</script>

comp
