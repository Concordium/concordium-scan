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
	<BWCubeLogoIcon
		v-else
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import {
	useTransactionQuery,
	useTransactionQueryByHash,
} from '~/queries/useTransactionQuery'
import { usePagination } from '~/composables/usePagination'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id?: string
	hash?: string
}

const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refHash = toRef(props, 'hash')
const transactionQueryResult = ref()
if (props.id)
	transactionQueryResult.value = useTransactionQuery(refId as Ref<string>, {
		first,
		last,
		after,
		before,
	})
else if (props.hash)
	transactionQueryResult.value = useTransactionQueryByHash(
		refHash as Ref<string>,
		{
			first,
			last,
			after,
			before,
		}
	)
</script>
