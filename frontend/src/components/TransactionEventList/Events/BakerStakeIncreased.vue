<template>
	<span>
		Baker <Baker :id="event.bakerId" /> increased stake to
		{{ convertMicroCcdToCcd(event.newStakedAmount) }}Ͼ
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import Baker from '~/components/molecules/Baker.vue'
import type { BakerStakeIncreased } from '~/types/generated'

type Props = {
	event: BakerStakeIncreased
}

defineProps<Props>()
</script>
