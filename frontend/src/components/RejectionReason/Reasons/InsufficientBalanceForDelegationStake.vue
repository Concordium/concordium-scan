<template>
	<span> Account balance was insufficient to cover the proposed stake </span>
</template>

<script setup lang="ts">
import type { InsufficientBalanceForDelegationStake } from '~/types/generated'

type Props = {
	reason: InsufficientBalanceForDelegationStake
}

defineProps<Props>()
</script>
