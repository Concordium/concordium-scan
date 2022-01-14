<template>
	<Drawer :is-open="!!selectedBlockId" :on-close="() => (selectedBlockId = '')">
		<BlockDetailsContent :id="selectedBlockId" />
		<DrawerActions>
			<Button :on-click="() => (selectedBlockId = '')" class="self-end">
				Close
			</Button>
		</DrawerActions>
	</Drawer>
</template>

<script lang="ts" setup>
import BlockDetailsContent from './BlockDetailsContent.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
const selectedBlockId = useBlockDetails()
</script>
