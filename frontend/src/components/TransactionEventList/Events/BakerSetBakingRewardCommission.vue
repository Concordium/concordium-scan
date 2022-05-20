<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> set baking reward commission to
		{{ convertMicroCcdToCcd(event.bakingRewardCommission) }}Ͼ</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetBakingRewardCommission } from '~/types/generated'
import { convertMicroCcdToCcd } from '~/utils/format'

type Props = {
	event: BakerSetBakingRewardCommission
}

defineProps<Props>()
</script>
