<template>
	<span>
		The account <Hash :hash="reason.accountAddress" /> does not exist
	</span>
</template>

<script setup lang="ts">
import Hash from '~/components/molecules/Hash.vue'
import type { InvalidAccountReference } from '~/types/generated'

type Props = {
	reason: InvalidAccountReference
}

defineProps<Props>()
</script>
