<template>
	<div v-if="transactionQueryResult.data">
		<TransactionDetailsContent
			v-if="transactionQueryResult.data.transaction"
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
import {
	useTransactionQuery,
	useTransactionQueryByHash,
} from '~/queries/useTransactionQuery'
import { usePagination } from '~/composables/usePagination'

const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id?: string
	hash?: string
}

const props = defineProps<Props>()
const transactionQueryResult = ref()
if (props.id)
	transactionQueryResult.value = useTransactionQuery(props.id, {
		first,
		last,
		after,
		before,
	})
else if (props.hash)
	transactionQueryResult.value = useTransactionQueryByHash(props.hash, {
		first,
		last,
		after,
		before,
	})
</script>
