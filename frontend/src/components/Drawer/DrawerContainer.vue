<template>
	<Drawer
		:is-open="!!currentTopItem?.id || !!currentTopItem?.hash"
		:on-close="() => softReset()"
	>
		<template #content>
			<BlockDetailsContainer
				v-if="currentTopItem && currentTopItem.entityTypeName == 'block'"
				:id="currentTopItem?.id"
				:hash="currentTopItem?.hash"
			/>
			<TransactionDetailsContainer
				v-if="currentTopItem && currentTopItem.entityTypeName == 'transaction'"
				:id="currentTopItem?.id"
				:hash="currentTopItem?.hash"
			/>
		</template>

		<template #actions>
			<DrawerActions>
				<Button class="self-end" :on-click="() => softReset()"> Close </Button>
			</DrawerActions>
		</template>
	</Drawer>
</template>

<script lang="ts" setup>
import { useDrawer } from '~/composables/useDrawer'
import Drawer from '~/components/Drawer/Drawer.vue'
const { softReset } = useDrawer()
const { currentTopItem } = useDrawer()
</script>
