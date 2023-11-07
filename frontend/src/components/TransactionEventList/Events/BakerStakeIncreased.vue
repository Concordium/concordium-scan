<template>
	<span>
		Validator <BakerLink :id="event.bakerId" /> increased stake to
		{{ convertMicroCcdToCcd(event.newStakedAmount) }}Ͼ
	</span>
</template>

<script setup lang="ts">
import { convertMicroCcdToCcd } from '~/utils/format'
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerStakeIncreased } from '~/types/generated'

type Props = {
	event: BakerStakeIncreased
}

defineProps<Props>()
</script>
