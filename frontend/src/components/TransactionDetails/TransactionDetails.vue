<template>
	<Drawer :is-open="!!props.transactionId" :on-close="props.onClose">
		<TransactionDetailsContent :id="props.transactionId" />
		<DrawerActions>
			<Button :on-click="props.onClose" class="self-end">Close</Button>
		</DrawerActions>
	</Drawer>
</template>

<script lang="ts" setup>
import TransactionDetailsContent from './TransactionDetailsContent.vue'
import Drawer from '~/components/Drawer/Drawer.vue'

type Props = {
	transactionId: string
	onClose: () => void
}

const props = defineProps<Props>()
</script>
