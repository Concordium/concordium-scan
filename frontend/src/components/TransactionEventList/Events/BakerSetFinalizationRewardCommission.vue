<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> set finalization reward commission
		to {{ convertMicroCcdToCcd(event.finalizationRewardCommission) }}Ͼ</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetFinalizationRewardCommission } from '~/types/generated'
import { convertMicroCcdToCcd } from '~/utils/format'

type Props = {
	event: BakerSetFinalizationRewardCommission
}

defineProps<Props>()
</script>
