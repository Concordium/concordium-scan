<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<TransactionIcon class="w-12 h-12 mr-4 none md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Transaction</h3>
			<h1
				v-if="$route.name != 'transactions-transactionHash'"
				class="font-mono inline-block text-2xl"
				:class="$style.title"
			>
				<div class="numerical truncate w-full">
					{{ transaction.transactionHash }}
				</div>
			</h1>
			<h1 v-else class="inline-block text-2xl" :class="$style.title">
				<span class="numerical truncate inline-block w-full">
					{{ transaction.transactionHash }}
				</span>
			</h1>
			<TextCopy
				:text="transaction.transactionHash"
				label="Click to copy transaction hash to clipboard"
				class="h-5 inline align-baseline mr-3"
				tooltip-class="font-sans"
			/>
			<Badge
				:type="
					transaction.result.__typename === 'Success' ? 'success' : 'failure'
				"
			>
				{{
					transaction.result.__typename === 'Success' ? 'Success' : 'Rejected'
				}}
			</Badge>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import Badge from '~/components/Badge.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import type { Transaction } from '~/types/transactions'

type Props = {
	transaction: Transaction
}

defineProps<Props>()
</script>

<style module>
.title {
	max-width: calc(100% - 200px);
}
</style>
