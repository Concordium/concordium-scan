<template>
	<div v-if="data?.transaction">
		<TransactionDetailsContent
			:transaction="data?.transaction"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { useTransactionQuery } from '~/queries/useTransactionQuery'
import { usePagination } from '~/composables/usePagination'

const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id: string
}

const props = defineProps<Props>()
const { data } = useTransactionQuery(props.id, { first, last, after, before })
</script>
