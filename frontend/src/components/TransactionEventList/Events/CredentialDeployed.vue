<template>
	<span>
		Deployed account with address
		<AccountLink :address="event.accountAddress.asString" /> from
		<Hash :hash="event.regId" />
	</span>
</template>

<script setup lang="ts">
import AccountLink from '../../molecules/AccountLink.vue'
import type { CredentialDeployed } from '~/types/generated'

type Props = {
	event: CredentialDeployed
}

defineProps<Props>()
</script>
