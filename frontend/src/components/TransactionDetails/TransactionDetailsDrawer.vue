<template>
	<Drawer :is-open="!!props.id"
		><!--TODO: :on-close="() => (props.id = '')"-->
		<template #content>
			<TransactionDetailsContainer :id="props.id" />
		</template>
		<template #actions>
			<DrawerActions>
				<Button class="self-end">
					<!--TODO:  :on-click="() => (props.id = '')"  -->
					Close
				</Button>
			</DrawerActions>
		</template>
	</Drawer>
</template>

<script lang="ts" setup>
import TransactionDetailsContainer from './TransactionDetailsContainer.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
type Props = {
	id: string
}
const props = defineProps<Props>()
</script>
