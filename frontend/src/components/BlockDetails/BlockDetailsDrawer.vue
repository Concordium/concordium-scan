<template>
	<Drawer :is-open="!!props.id" :on-close="() => softReset()"
		><!--TODO: :on-close="() => (props.id = '')"-->
		<template #content>
			<BlockDetailsContainer :id="props.id" />
		</template>

		<template #actions>
			<DrawerActions>
				<Button class="self-end" :on-click="() => softReset()"
					><!--TODO: :on-click="() => (props.id = '')" -->
					Close
				</Button>
			</DrawerActions>
		</template>
	</Drawer>
</template>

<script lang="ts" setup>
import BlockDetailsContainer from './BlockDetailsContainer.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
const { softReset } = useDrawer()
type Props = {
	id: string
}
const props = defineProps<Props>()
</script>
