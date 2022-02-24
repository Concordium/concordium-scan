<template>
	<span>
		Deployed account with address
		<AccountLink :address="event.accountAddress" /> from
		<Hash :hash="event.regId" />
	</span>
</template>

<script setup lang="ts">
import type { CredentialDeployed } from '~/types/generated'

type Props = {
	event: CredentialDeployed
}

defineProps<Props>()
</script>
