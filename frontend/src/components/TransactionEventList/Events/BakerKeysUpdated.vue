<template>
	<span> Validator <BakerLink :id="event.bakerId" /> keys were updated</span>
</template>

<script setup lang="ts">
import BakerLink from '~/components/molecules/BakerLink.vue'
import type { BakerKeysUpdated } from '~/types/generated'

type Props = {
	event: BakerKeysUpdated
}

defineProps<Props>()
</script>
