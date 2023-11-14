<template>
	<span>
		Block reward commission is not in the valid range for a validator
	</span>
</template>

<script setup lang="ts">
import type { BakingRewardCommissionNotInRange } from '~/types/generated'

type Props = {
	reason: BakingRewardCommissionNotInRange
}

defineProps<Props>()
</script>
