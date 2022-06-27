<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<NodeIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<div class="flex flex-col">
				<h3 class="w-full text-sm text-theme-faded">Node</h3>
				<h1 class="inline-block text-2xl">
					{{ node.nodeName }}
				</h1>
				<div class="flex items-center">{{ node.nodeId }}</div>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import type { NodeStatus } from '~/types/generated'
import NodeIcon from '~/components/icons/NodeIcon.vue'

type Props = {
	node: NodeStatus
}

const props = defineProps<Props>()
</script>
