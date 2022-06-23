<template>
	<div class="w-full">
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Hash</TableTh>
					<TableTh>Type</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Sender</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG" align="right">
						Cost (Ͼ)
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="transaction in transactions"
					:key="transaction.transactionHash"
				>
					<TableTd class="numerical">
						<TransactionResult :result="transaction.result" />
						<TransactionLink
							:id="transaction.id"
							:hash="transaction.transactionHash"
						/>
					</TableTd>
					<TableTd>
						<div class="whitespace-normal">
							{{ translateTransactionType(transaction.transactionType) }}
						</div>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG" class="numerical">
						<AccountLink
							:address="transaction.senderAccountAddress?.asString"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						align="right"
						class="numerical"
					>
						<Amount :amount="transaction.ccdCost" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PageInfo, Transaction } from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import TransactionResult from '~/components/molecules/TransactionResult.vue'

const { breakpoint } = useBreakpoint()

type Props = {
	transactions: Transaction[]
	totalCount: number
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
