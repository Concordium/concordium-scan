<template>
	<span>
		Amount would result in pool capital higher than the maximum threshold
	</span>
</template>

<script setup lang="ts">
import type { StakeOverMaximumThresholdForPool } from '~/types/generated'

type Props = {
	reason: StakeOverMaximumThresholdForPool
}

defineProps<Props>()
</script>
