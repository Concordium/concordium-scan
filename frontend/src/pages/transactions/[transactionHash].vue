<template>
	<div v-if="$route.params.internalId">
		<div v-if="transactionQueryResult.data">
			<TransactionDetailsContent
				:transaction="transactionQueryResult.data.transaction"
			/>
		</div>
	</div>
	<div v-else>
		<div v-if="transactionQueryResult.data">
			<TransactionDetailsContent
				:transaction="transactionQueryResult.data.transactionByTransactionHash"
			/>
		</div>
	</div>
</template>
<script lang="ts" setup>
import TransactionDetailsContent from '../../components/TransactionDetails/TransactionDetailsContent.vue'
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
