<template>
	<span> Baker <Baker :id="event.bakerId" /> was scheduled to be removed </span>
</template>

<script setup lang="ts">
import Baker from '~/components/molecules/Baker.vue'
import type { BakerRemoved } from '~/types/generated'

type Props = {
	event: BakerRemoved
}

defineProps<Props>()
</script>
