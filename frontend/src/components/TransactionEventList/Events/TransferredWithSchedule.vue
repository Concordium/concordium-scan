<template>
	<span>
		Transferred
		<span class="numerical">{{ convertMicroCcdToCcd(event.totalAmount) }}</span>
		Ͼ from account <AccountLink :address="event.fromAccountAddressString" /> to
		account <AccountLink :address="event.toAccountAddressString" /> with a
		release schedule
		<Button @click="showSchedule = !showSchedule">
			<span v-if="!showSchedule">expand</span
			><span v-if="showSchedule">hide</span>
		</Button>

		<TransactionDetailsReleaseSchedule
			v-if="
				showSchedule &&
				data?.transactionByTransactionHash?.result?.events?.nodes[0]
			"
			:go-to-page="goToPageReleaseSchedule"
			:page-info="
				data?.transactionByTransactionHash.result.events.nodes[0]
					.amountsSchedule.pageInfo
			"
			:release-schedule-items="
				data?.transactionByTransactionHash.result.events.nodes[0]
					.amountsSchedule.nodes
			"
		>
		</TransactionDetailsReleaseSchedule>
	</span>
</template>
<script setup lang="ts">
import type { Transaction, TransferredWithSchedule } from '~/types/generated'
import { convertMicroCcdToCcd } from '~/utils/format'
import { useTransactionReleaseSchedule } from '~/queries/useTransactionReleaseSchedule'
import TransactionDetailsReleaseSchedule from '~/components/TransactionDetails/TransactionDetailsReleaseSchedule.vue'
import { PAGE_SIZE_SMALL } from '~/composables/usePagination'
type Props = {
	event: TransferredWithSchedule
	transaction: Transaction
}
const {
	first,
	last,
	after,
	before,
	goToPage: goToPageReleaseSchedule,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const showSchedule = ref(false)
const props = defineProps<Props>()
const transactionHashRef = ref(props.transaction.transactionHash)
const { data } = useTransactionReleaseSchedule(transactionHashRef, {
	first,
	last,
	after,
	before,
})
</script>
