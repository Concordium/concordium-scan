<template>
	<Drawer :is-open="!!selectedTxId" :on-close="() => (selectedTxId = '')">
		<TransactionDetailsContent :id="selectedTxId" />
		<DrawerActions>
			<Button :on-click="() => (selectedTxId = '')" class="self-end">
				Close
			</Button>
		</DrawerActions>
	</Drawer>
</template>

<script lang="ts" setup>
import TransactionDetailsContent from './TransactionDetailsContent.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
const selectedTxId = useTransactionDetails()
</script>
