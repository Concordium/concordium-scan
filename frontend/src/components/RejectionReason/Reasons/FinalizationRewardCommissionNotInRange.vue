<template>
	<span
		>Finalization reward commission is not in the valid range for a validator
	</span>
</template>

<script setup lang="ts">
import type { FinalizationRewardCommissionNotInRange } from '~/types/generated'

type Props = {
	reason: FinalizationRewardCommissionNotInRange
}

defineProps<Props>()
</script>
