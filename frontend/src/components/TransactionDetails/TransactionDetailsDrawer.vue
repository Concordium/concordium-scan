<template>
	<Drawer :is-open="!!props.id" :on-close="() => softReset()">
		<template #content>
			<TransactionDetailsContainer :id="props.id" />
		</template>
		<template #actions>
			<DrawerActions>
				<Button class="self-end" :on-click="() => softReset()"> Close </Button>
			</DrawerActions>
		</template>
	</Drawer>
</template>

<script lang="ts" setup>
import TransactionDetailsContainer from './TransactionDetailsContainer.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
const { softReset } = useDrawer()
type Props = {
	id: string
}
const props = defineProps<Props>()
</script>
