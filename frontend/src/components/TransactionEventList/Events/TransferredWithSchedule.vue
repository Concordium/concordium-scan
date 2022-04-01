<template>
	<span>
		Transferred
		<span class="numerical">{{ convertMicroCcdToCcd(event.totalAmount) }}</span>
		Ͼ from account <AccountLink :address="event.fromAccountAddressString" /> to
		account <AccountLink :address="event.toAccountAddressString" /> with a
		release schedule.

		<button
			class="justify-between items-center rounded-lg inline-block p-1 px-2 mx-2 text-xs bg-theme-background-primary-elevated hover:bg-theme-background-primary-elevated-hover"
			@click="showSchedule = !showSchedule"
		>
			{{ showMoreText }}
			<ChevronForwardIcon
				class="w-3 h-3 inline align-middle"
				:class="[{ 'icon-open': showSchedule }]"
				aria-hidden
			/>
		</button>

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
			class="mt-4"
		/>
	</span>
</template>
<script setup lang="ts">
import type { Transaction, TransferredWithSchedule } from '~/types/generated'
import { convertMicroCcdToCcd } from '~/utils/format'
import { useTransactionReleaseSchedule } from '~/queries/useTransactionReleaseSchedule'
import TransactionDetailsReleaseSchedule from '~/components/TransactionDetails/TransactionDetailsReleaseSchedule.vue'
import ChevronForwardIcon from '~/components/icons/ChevronForwardIcon.vue'
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

const showMoreText = computed(() =>
	showSchedule.value ? 'Show less' : 'Show more'
)
</script>

<style scoped>
.icon-open {
	transform: rotate(90deg);
}
</style>
