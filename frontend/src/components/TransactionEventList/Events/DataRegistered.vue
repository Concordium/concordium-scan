<template>
	<span>Data was registered</span>
</template>

<script setup lang="ts">
import type { DataRegistered } from '~/types/generated'

type Props = {
	event: DataRegistered
}

defineProps<Props>()
</script>
