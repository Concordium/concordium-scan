<template>
	<span>
		Baker <BakerLink :id="event.bakerId" /> set metadata url to
		{{ event.metadataUrl }}</span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetMetadataUrl } from '~/types/generated'

type Props = {
	event: BakerSetMetadataUrl
}

defineProps<Props>()
</script>
