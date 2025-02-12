<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<BlockIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Block</h3>
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl numerical">
					{{ shortenHash(block?.blockHash) }}
				</h1>
				<TextCopy
					:text="block.blockHash"
					label="Click to copy block hash to clipboard"
					class="mx-3"
					icon-size="h-5 w-5"
					tooltip-class="font-sans"
				/>
				<h1 class="inline-block text-2xl numerical">
					{{ String(block?.id) }}
				</h1>
				<TextCopy
					:text="String(block.id)"
					label="Click to copy block height to clipboard"
					class="mx-3"
					icon-size="h-5 w-5"
					tooltip-class="font-sans"
				/>
				<Badge :type="block.finalized ? 'success' : 'failure'">
					{{ block?.finalized ? 'Finalized' : 'Rejected' }}
				</Badge>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import BlockIcon from '~/components/icons/BlockIcon.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import Badge from '~/components/Badge.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import { shortenHash } from '~/utils/format'
import type { Block } from '~/types/generated'

type Props = {
	block: Block
}

defineProps<Props>()
</script>
