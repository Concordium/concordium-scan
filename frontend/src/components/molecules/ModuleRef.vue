<template>
	<Tooltip :text="moduleRef" text-class="text-theme-body">
		<CodeIcon class="h-4 inline align-baseline" :class="iconClass" />
		{{ shortenHash(moduleRef) }}
	</Tooltip>
</template>

<script setup lang="ts">
import { CodeIcon } from '@heroicons/vue/solid/index.js'
import { shortenHash } from '~/utils/format'

type Props = {
	moduleRef: string
	iconClass?: string
}

defineProps<Props>()
</script>
