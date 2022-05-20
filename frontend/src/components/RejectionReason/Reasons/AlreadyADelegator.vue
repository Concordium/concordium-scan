<template>
	<span> Account already has a delegator </span>
</template>

<script setup lang="ts">
import type { AlreadyADelegator } from '~/types/generated'

type Props = {
	reason: AlreadyADelegator
}

defineProps<Props>()
</script>
