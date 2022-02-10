<template>
	<Tooltip :text="hash" text-class="text-theme-body">
		<HashtagIcon class="h-4 inline align-baseline" :class="iconClass" />
		{{ shortenHash(hash) }}
	</Tooltip>
</template>

<script setup lang="ts">
import { HashtagIcon } from '@heroicons/vue/solid/index.js'
import { shortenHash } from '~/utils/format'

type Props = {
	hash: string
	iconClass?: string
}

defineProps<Props>()
</script>
