<template>
	<div>
		<Title>CCDScan | Transactions</Title>
		<main class="p-4 pb-0">
			<Table>
				<TableHead>
					<TableRow>
						<TableTh width="10%">Status</TableTh>
						<TableTh width="20%">Timestamp</TableTh>
						<TableTh width="30%">Type</TableTh>
						<TableTh width="10%">Transaction hash</TableTh>
						<TableTh width="10%">Block height</TableTh>
						<TableTh width="10%">Sender</TableTh>
						<TableTh width="10%" align="right">Cost (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow>
						<TableTd colspan="6" align="center" class="p-0 tdlol">
							<ShowMoreButton :new-item-count="newItems" :refetch="refetch" />
						</TableTd>
					</TableRow>
					<TableRow
						v-for="transaction in pagedData"
						:key="transaction.transactionHash"
					>
						<TableTd>
							<StatusCircle
								:class="[
									'h-4 mr-2 text-theme-interactive',
									{ 'text-theme-error': !transaction.result.successful },
								]"
							/>
							{{ transaction.result.successful ? 'Success' : 'Rejected' }}
						</TableTd>
						<TableTd>
							{{ convertTimestampToRelative(transaction.block.blockSlotTime) }}
						</TableTd>
						<TableTd>
							{{ translateTransactionType(transaction.transactionType) }}
						</TableTd>
						<TableTd>
							<HashtagIcon :class="$style.cellIcon" />
							<LinkButton
								:class="$style.numerical"
								@click="
									drawer.push(
										'transaction',
										transaction.transactionHash,
										transaction.id
									)
								"
							>
								{{ transaction.transactionHash.substring(0, 6) }}
							</LinkButton>
						</TableTd>
						<TableTd :class="$style.numerical">
							{{ transaction.block.blockHeight }}
						</TableTd>
						<TableTd :class="$style.numerical">
							<UserIcon
								v-if="transaction.senderAccountAddress"
								:class="$style.cellIcon"
							/>
							{{ transaction.senderAccountAddress?.substring(0, 6) }}
						</TableTd>
						<TableTd align="right" :class="$style.numerical">
							{{ convertMicroCcdToCcd(transaction.ccdCost) }}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>

			<LoadMore
				v-if="data?.transactions.pageInfo"
				:page-info="data?.transactions.pageInfo"
				:on-load-more="loadMore"
			/>
		</main>
	</div>
</template>

<script lang="ts" setup>
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid/index.js'
import {
	convertMicroCcdToCcd,
	convertTimestampToRelative,
} from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { usePagedData } from '~/composables/usePagedData'
import { useTransactionsListQuery } from '~/queries/useTransactionListQuery'
import { useBlockSubscription } from '~/subscriptions/useBlockSubscription'
import type { BlockSubscriptionResponse } from '~/types/blocks'
import type { Transaction } from '~/types/transactions'

const { pagedData, first, last, after, addPagedData, fetchNew, loadMore } =
	usePagedData<Transaction>()

const newItems = ref(0)
const subscriptionHandler = (
	_prevData: void,
	newData: BlockSubscriptionResponse
) => {
	newItems.value += newData.blockAdded.transactionCount
}

const before = ref<string | undefined>(undefined)

const drawer = useDrawer()

useBlockSubscription(subscriptionHandler)

const refetch = () => {
	fetchNew(newItems.value)
	newItems.value = 0
}

const { data } = useTransactionsListQuery({
	first,
	last,
	after,
	before,
})

watch(
	() => data.value,
	value => {
		addPagedData(value?.transactions.nodes || [], value?.transactions.pageInfo)
	}
)
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}

.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
