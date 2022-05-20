<template>
	<span> Delegator is in cooldown </span>
</template>

<script setup lang="ts">
import type { DelegatorInCooldown } from '~/types/generated'

type Props = {
	reason: DelegatorInCooldown
}

defineProps<Props>()
</script>
