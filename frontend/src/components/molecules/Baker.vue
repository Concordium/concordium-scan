<template>
	<span>
		<UserIcon class="h-4 inline align-baseline" :class="iconClass" />
		{{ id }}
	</span>
</template>

<script setup lang="ts">
import { UserIcon } from '@heroicons/vue/solid/index.js'

type Props = {
	id: string
	iconClass?: string
}

defineProps<Props>()
</script>
