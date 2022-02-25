<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<BlockIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Block</h3>
			<h1
				v-if="$route.name != 'blocks-blockHash'"
				class="inline-block text-2xl"
				:class="$style.title"
			>
				<div class="numerical truncate w-full">
					{{ block.blockHash }}
				</div>
			</h1>
			<h1 v-else class="inline-block text-2xl" :class="$style.title">
				<span class="numerical truncate inline-block w-full">
					{{ block.blockHash }}
				</span>
			</h1>
			<div>
				<TextCopy
					:text="block.blockHash"
					label="Click to copy block hash to clipboard"
					class="h-5 inline align-baseline mr-3"
					tooltip-class="font-sans"
				/>
				<Badge :type="block.finalized ? 'success' : 'failure'">
					{{ block?.finalized ? 'Finalised' : 'Rejected' }}
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
import type { Block } from '~/types/blocks'

type Props = {
	block: Block
}

defineProps<Props>()
</script>

<style module>
.title {
	max-width: calc(100% - 200px);
}
</style>
