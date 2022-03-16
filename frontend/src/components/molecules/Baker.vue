<template>
	<div v-if="id || id === 0" class="inline-block">
		<UserIcon class="h-4 inline align-text-top" :class="iconClass" />
		<span>{{ id }}</span>
	</div>
</template>

<script setup lang="ts">
import { UserIcon } from '@heroicons/vue/solid/index.js'

type Props = {
	id?: number | null
	iconClass?: string
}

defineProps<Props>()
</script>
