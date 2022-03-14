<template>
	<div v-if="transactionQueryResult.data">
		<TransactionDetailsContent
			v-if="$route.params.internalId && transactionQueryResult.data.transaction"
			:transaction="transactionQueryResult.data.transaction"
			:go-to-page="goToPage"
		/>
		<TransactionDetailsContent
			v-else
			:transaction="transactionQueryResult.data.transactionByTransactionHash"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { Ref } from 'vue'
import TransactionDetailsContent from '~/components/TransactionDetails/TransactionDetailsContent.vue'
import {
	useTransactionQueryByHash,
	useTransactionQuery,
} from '~/queries/useTransactionQuery'
import { usePagination } from '~/composables/usePagination'

const { first, last, after, before, goToPage } = usePagination()

const route = useRoute()
const transactionQueryResult = ref()
const transactionHashRef = ref(route.params.transactionHash)
const internalIdRef = ref(route.params.internalId)
if (!route.params.internalId)
	transactionQueryResult.value = useTransactionQueryByHash(
		transactionHashRef as Ref<string>,
		{ first, last, after, before }
	)
else
	transactionQueryResult.value = useTransactionQuery(
		internalIdRef as Ref<string>,
		{ first, last, after, before }
	)
</script>
