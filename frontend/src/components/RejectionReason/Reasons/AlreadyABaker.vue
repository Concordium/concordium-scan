<template>
	<span> Baker <Baker :id="reason.bakerId" /> is already a baker </span>
</template>

<script setup lang="ts">
import Baker from '~/components/molecules/Baker.vue'
import type { AlreadyABaker } from '~/types/generated'

type Props = {
	reason: AlreadyABaker
}

defineProps<Props>()
</script>
