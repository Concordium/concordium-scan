<template>
	<span>
		Validator <BakerLink :id="event.bakerId" /> set block reward commission to
		{{ event.bakingRewardCommission * 100 }}%</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetBakingRewardCommission } from '~/types/generated'

type Props = {
	event: BakerSetBakingRewardCommission
}

defineProps<Props>()
</script>
