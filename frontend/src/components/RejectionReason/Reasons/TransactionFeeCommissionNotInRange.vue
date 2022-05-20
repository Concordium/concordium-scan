<template>
	<span>
		Transaction fee commission is not in the valid range for a baker
	</span>
</template>

<script setup lang="ts">
import type { TransactionFeeCommissionNotInRange } from '~/types/generated'

type Props = {
	reason: TransactionFeeCommissionNotInRange
}

defineProps<Props>()
</script>
