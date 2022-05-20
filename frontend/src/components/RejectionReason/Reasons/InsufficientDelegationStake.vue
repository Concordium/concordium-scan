<template>
	<span> Insufficient delegation stake </span>
</template>

<script setup lang="ts">
import type { InsufficientDelegationStake } from '~/types/generated'

type Props = {
	reason: InsufficientDelegationStake
}

defineProps<Props>()
</script>
