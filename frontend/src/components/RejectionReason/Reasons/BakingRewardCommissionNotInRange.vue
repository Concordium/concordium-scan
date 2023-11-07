<template>
	<span>
		Validation reward commission is not in the valid range for a baker
	</span>
</template>

<script setup lang="ts">
import type { BakingRewardCommissionNotInRange } from '~/types/generated'

type Props = {
	reason: BakingRewardCommissionNotInRange
}

defineProps<Props>()
</script>
