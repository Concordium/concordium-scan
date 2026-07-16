<template>
	<div v-if="props.lockId" class="inline-block whitespace-nowrap">
		<LockClosedIcon class="h-4 w-4 text-theme-white inline align-text-top" />
		<span class="numerical px-2">
			<Tooltip :text="props.lockId" text-class="text-theme-body">
				{{ shortenHash(props.lockId) }}
			</Tooltip>
		</span>
		<TextCopy
			:text="props.lockId"
			label="Click to copy lock ID to clipboard"
			class="h-5 inline align-baseline"
			tooltip-class="font-sans"
		/>
	</div>
</template>

<script lang="ts" setup>
import { LockClosedIcon } from '@heroicons/vue/solid/index.js'
import { shortenHash } from '~/utils/format'
import TextCopy from '~/components/atoms/TextCopy.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'

type Props = {
	lockId?: string | null
}

const props = defineProps<Props>()
</script>
