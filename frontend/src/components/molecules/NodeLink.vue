<template>
	<div class="inline-block">
		<NodeIcon class="h-4 inline align-text-top" :class="iconClass" />
		{{ node.nodeName }}
	</div>
</template>

<script setup lang="ts">
import { useDrawer } from '~/composables/useDrawer'
import LinkButton from '~/components/atoms/LinkButton.vue'
import type { NodeStatus } from '~/types/generated'
import NodeIcon from '~/components/icons/NodeIcon.vue'

type Props = {
	node: NodeStatus
	iconClass?: string
}

defineProps<Props>()

const drawer = useDrawer()

const handleOnClick = (nodeId: string) => {
	drawer.push({ entityTypeName: 'node', nodeId })
}
</script>
