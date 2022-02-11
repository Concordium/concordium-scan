<template>
	<span>
		Account created with address
		<Hash :hash="event.accountAddress" />
	</span>
</template>

<script setup lang="ts">
import Hash from '~/components/molecules/Hash.vue'
import type { AccountCreated } from '~/types/generated'

type Props = {
	event: AccountCreated
}

defineProps<Props>()
</script>
