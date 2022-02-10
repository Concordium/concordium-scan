<template>
	<span> Baker <Baker :id="event.bakerId" /> keys were updated</span>
</template>

<script setup lang="ts">
import Baker from '~/components/molecules/Baker.vue'
import type { BakerKeysUpdated } from '~/types/generated'

type Props = {
	event: BakerKeysUpdated
}

defineProps<Props>()
</script>
