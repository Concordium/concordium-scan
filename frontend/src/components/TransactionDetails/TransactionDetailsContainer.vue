<template>
	<div v-if="data?.transaction">
		<TransactionDetailsContent :transaction="data.transaction" />
	</div>
</template>

<script lang="ts" setup>
import { useTransactionQuery } from '~/queries/useTransactionQuery'

type Props = {
	id: string
}
const props = defineProps<Props>()
const { data } = useTransactionQuery(props.id)
</script>
