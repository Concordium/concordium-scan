<template>
	<Drawer :is-open="!!selectedBlockId" :on-close="() => (selectedBlockId = '')">
		<template #content>
			<BlockDetailsContainer :id="selectedBlockId" />
		</template>

		<template #actions>
			<DrawerActions>
				<Button :on-click="() => (selectedBlockId = '')" class="self-end">
					Close
				</Button>
			</DrawerActions>
		</template>
	</Drawer>
</template>

<script lang="ts" setup>
import BlockDetailsContainer from './BlockDetailsContainer.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
const selectedBlockId = useBlockDetails()
</script>
