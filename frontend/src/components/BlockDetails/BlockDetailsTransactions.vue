<template>
	<div class="w-full">
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Status / Hash</TableTh>
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
						<StatusCircle
							:class="[
								'h-4 w-6 mr-2 text-theme-interactive',
								{
									'text-theme-error':
										transaction.result.__typename === 'Rejected',
								},
							]"
						/>

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
						<AccountLink :address="transaction.senderAccountAddress" />
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						align="right"
						class="numerical"
					>
						{{ convertMicroCcdToCcd(transaction.ccdCost) }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination
			v-if="pageInfo && totalCount > PAGE_SIZE"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/generated'

const { breakpoint } = useBreakpoint()

type Props = {
	transactions: Transaction[]
	totalCount: number
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
