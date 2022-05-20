a
<template>
	<span> Pool is not open to delegators </span>
</template>

<script setup lang="ts">
import type { PoolClosed } from '~/types/generated'

type Props = {
	reason: PoolClosed
}

defineProps<Props>()
</script>
