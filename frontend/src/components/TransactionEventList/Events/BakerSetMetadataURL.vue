<template>
	<span>
		Validator <BakerLink :id="event.bakerId" /> set metadata url to
		<ExternalLink :href="event.metadataUrl">{{
			event.metadataUrl
		}}</ExternalLink></span
	>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerSetMetadataUrl } from '~/types/generated'
import ExternalLink from '~/components/molecules/ExternalLink.vue'

type Props = {
	event: BakerSetMetadataUrl
}

defineProps<Props>()
</script>
