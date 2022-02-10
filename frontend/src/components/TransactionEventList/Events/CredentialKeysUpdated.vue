<template>
	<span>Credential keys for {{ event.credId }} was updated</span>
</template>

<script setup lang="ts">
import type { CredentialKeysUpdated } from '~/types/generated'

type Props = {
	event: CredentialKeysUpdated
}

defineProps<Props>()
</script>
