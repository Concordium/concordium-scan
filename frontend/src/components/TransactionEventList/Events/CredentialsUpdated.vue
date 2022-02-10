<template>
	<span>
		Account <Hash :hash="event.accountAddress" /> updated the credentials
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
