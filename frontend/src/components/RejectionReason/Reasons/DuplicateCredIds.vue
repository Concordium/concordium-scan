<template>
	<span> Duplicate credentials ({{ reason.credIds.join(', ') }})</span>
</template>

<script setup lang="ts">
import type { DuplicateCredIds } from '~/types/generated'

type Props = {
	reason: DuplicateCredIds
}

defineProps<Props>()
</script>
