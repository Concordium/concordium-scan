<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> decreased stake to
		{{ convertMicroCcdToCcd(event.newStakedAmount) }}Ͼ
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerStakeDecreased } from '~/types/generated'

type Props = {
	event: BakerStakeDecreased
}

defineProps<Props>()
</script>
