<template>
	<span>
		Baker <Baker :id="event.bakerId" /> decreased stake to
		{{ convertMicroCcdToCcd(event.newStakedAmount) }}Ͼ
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import Baker from '~/components/molecules/Baker.vue'
import type { BakerStakeDecreased } from '~/types/generated'

type Props = {
	event: BakerStakeDecreased
}

defineProps<Props>()
</script>
