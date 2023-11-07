<template>
	<span>
		Validator <BakerLink :id="event.bakerId" /> set open status to
		{{ translateBakerPoolOpenStatus(event.openStatus) }}</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetOpenStatus } from '~/types/generated'
import { translateBakerPoolOpenStatus } from '~/utils/translateBakerPoolOpenStatus'
type Props = {
	event: BakerSetOpenStatus
}

defineProps<Props>()
</script>
