<template>
	<div class="inline-block">
		<Tooltip :text="hash" text-class="text-theme-body">
			<HashtagIcon class="h-4 inline align-middle" :class="iconClass" />
			<span>{{ shortenHash(hash) }}</span>
		</Tooltip>
		<TextCopy
			:text="hash"
			label="Click to copy to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
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
