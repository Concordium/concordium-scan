<template>
	<div v-if="id || id === 0">
		<UserIcon class="h-4 inline align-text-top" :class="iconClass" />
		<span>{{ id }}</span>
	</div>
</template>

<script setup lang="ts">
import { UserIcon } from '@heroicons/vue/solid/index.js'

type Props = {
	id?: number
	iconClass?: string
}

defineProps<Props>()
</script>
