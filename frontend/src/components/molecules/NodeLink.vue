<template>
	<div class="inline-block">
		<NodeIcon class="h-4 inline align-text-top" :class="iconClass" />
		<LinkButton class="px-2" @click="() => handleOnClick(node.id)">
			{{ node.nodeName }}
		</LinkButton>
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
	// TODO: This is a temporarily fix to ensure new backend is compatible with dotnet backend id format
	let actualNodeId: string = nodeId;
	try {
		const decoded = atob(nodeId);
		if (decoded.startsWith("NodeStatus")) {
			const trimmedNodeId = decoded.substring("NodeStatus".length).trim();
			if (trimmedNodeId.startsWith("d")) {
				actualNodeId = trimmedNodeId.substring(1).trim();
			}
		}
	} catch (error) {
		// If decoding fails, assume nodeId was not base64 encoded and keep the original value.
	}
	drawer.push({ entityTypeName: 'node', nodeId: actualNodeId });
}
</script>
