<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> set finalization reward commission
		to {{ event.finalizationRewardCommission * 100 }}%</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetFinalizationRewardCommission } from '~/types/generated'

type Props = {
	event: BakerSetFinalizationRewardCommission
}

defineProps<Props>()
</script>
