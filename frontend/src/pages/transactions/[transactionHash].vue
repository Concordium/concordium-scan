<template>
	<div v-if="transactionQueryResult.data">
		<TransactionDetailsContent
			v-if="$route.params.internalId && transactionQueryResult.data.transaction"
			:transaction="transactionQueryResult.data.transaction"
		/>
		<TransactionDetailsContent
			v-else
			:transaction="transactionQueryResult.data.transactionByTransactionHash"
		/>
	</div>
</template>
<script lang="ts" setup>
import TransactionDetailsContent from '~/components/TransactionDetails/TransactionDetailsContent.vue'
import {
	useTransactionQueryByHash,
	useTransactionQuery,
} from '~/queries/useTransactionQuery'
const route = useRoute()
const transactionQueryResult = ref()
if (!route.params.internalId)
	transactionQueryResult.value = useTransactionQueryByHash(
		route.params.transactionHash + ''
	)
else
	transactionQueryResult.value = useTransactionQuery(
		route.params.internalId + ''
	)
</script>
