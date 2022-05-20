<template>
	<span>
		Amount would result in pool with a too high fraction of delegated capital
	</span>
</template>

<script setup lang="ts">
import type { PoolWouldBecomeOverDelegated } from '~/types/generated'

type Props = {
	reason: PoolWouldBecomeOverDelegated
}

defineProps<Props>()
</script>
