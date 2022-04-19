<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> was scheduled to be removed
	</span>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerRemoved } from '~/types/generated'

type Props = {
	event: BakerRemoved
}

defineProps<Props>()
</script>
