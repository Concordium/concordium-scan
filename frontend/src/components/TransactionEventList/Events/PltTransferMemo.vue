<template>
	<div
		v-if="memo?.__typename === 'Memo'"
		class="bg-gray rounded flex items-center gap-2"
	>
		<span>Memo:</span>
		<span class="break-all whitespace-pre-line text-xs">{{
			memo?.bytes ? Buffer.from(memo.bytes, 'hex').toString('utf-8') : ''
		}}</span>
	</div>
</template>

<script setup lang="ts">
import { Buffer } from 'buffer'

import type { Memo } from '../../../types/generated'

type Props = {
	memo?: Memo
}

defineProps<Props>()
</script>
