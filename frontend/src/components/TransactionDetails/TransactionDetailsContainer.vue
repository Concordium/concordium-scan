<template>
	<div v-if="data?.transaction">
		<TransactionDetailsContent
			:transaction="transaction"
			:load-more-events="loadMore"
		/>
	</div>
</template>

<script lang="ts" setup>
import { useTransactionQuery } from '~/queries/useTransactionQuery'
import { usePagedData } from '~/composables/usePagedData'
import type {
	Transaction,
	TransactionSuccessfulEvent,
} from '~/types/transactions'

const { pagedData, first, last, after, before, addPagedData, loadMore } =
	usePagedData<TransactionSuccessfulEvent>()

type Props = {
	id: string
}

const props = defineProps<Props>()
const { data } = useTransactionQuery(props.id, { first, last, after, before })

const transaction = ref<Transaction>()

watch(
	() => data.value,
	value => {
		if (value) {
			transaction.value = value.transaction
		}

		if (value?.transaction.result.successful) {
			addPagedData(
				value?.transaction.result.events.nodes || [],
				value?.transaction.result.events.pageInfo
			)
		}
	}
)

watch(
	() => pagedData.value,
	value => {
		if (transaction.value && transaction.value.result.successful) {
			transaction.value = {
				...transaction.value,
				result: {
					...transaction.value?.result,
					events: {
						...transaction.value?.result.events,
						pageInfo: transaction.value.result.events.pageInfo,
						nodes: value,
					},
				},
			}
		}
	}
)
</script>
