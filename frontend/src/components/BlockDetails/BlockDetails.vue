<template>
	<Drawer :is-open="!!props.blockId" :on-close="props.onClose">
		<BlockDetailsContent :id="props.blockId" />
		<DrawerActions>
			<Button :on-click="props.onClose" class="self-end">Close</Button>
		</DrawerActions>
	</Drawer>
</template>

<script lang="ts" setup>
import BlockDetailsContent from './BlockDetailsContent.vue'
import Drawer from '~/components/Drawer/Drawer.vue'

type Props = {
	blockId: string
	onClose: () => void
}

const props = defineProps<Props>()
</script>
