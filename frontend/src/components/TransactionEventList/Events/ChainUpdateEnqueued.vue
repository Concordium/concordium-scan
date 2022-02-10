<template>
	<span> Chain update enqueued </span>
</template>

<script setup lang="ts">
import type { ChainUpdateEnqueued } from '~/types/generated'

type Props = {
	event: ChainUpdateEnqueued
}

defineProps<Props>()
</script>
