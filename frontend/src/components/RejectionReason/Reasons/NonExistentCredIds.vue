<template>
	<span>
		One or more credentials does not exist ({{
			reason.credIds.join(', ')
		}})</span
	>
</template>

<script setup lang="ts">
import type { NonExistentCredIds } from '~/types/generated'

type Props = {
	reason: NonExistentCredIds
}

defineProps<Props>()
</script>
