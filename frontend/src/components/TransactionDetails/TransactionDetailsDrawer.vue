<template>
	<Drawer :is-open="!!selectedTxId" :on-close="() => (selectedTxId = '')">
		<template #content>
			<TransactionDetailsContainer :id="selectedTxId" />
		</template>
		<template #actions>
			<DrawerActions>
				<Button :on-click="() => (selectedTxId = '')" class="self-end">
					Close
				</Button>
			</DrawerActions>
		</template>
	</Drawer>
</template>

<script lang="ts" setup>
import TransactionDetailsContainer from './TransactionDetailsContainer.vue'
import Drawer from '~/components/Drawer/Drawer.vue'
const selectedTxId = useTransactionDetails()
</script>
