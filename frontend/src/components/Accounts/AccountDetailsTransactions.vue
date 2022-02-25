<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Status</TableTh>
					<TableTh>Transaction hash</TableTh>
					<TableTh>Sender</TableTh>
					<TableTh align="right">Cost (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="transaction in transactions"
					:key="transaction.transaction.transactionHash"
				>
					<TableTd>
						<StatusCircle
							:class="[
								'h-4 w-6 mr-2 text-theme-interactive',
								{
									'text-theme-error':
										transaction.transaction.result.__typename === 'Rejected',
								},
							]"
						/>
						{{
							transaction.transaction.result.__typename === 'Success'
								? 'Success'
								: 'Rejected'
						}}
					</TableTd>
					<TableTd class="numerical">
						<TransactionLink
							:id="transaction.transaction.id"
							:hash="transaction.transaction.transactionHash"
						/>
					</TableTd>
					<TableTd class="numerical">
						<AccountLink
							:address="transaction.transaction.senderAccountAddress"
						/>
					</TableTd>
					<TableTd align="right" class="numerical">
						{{ convertMicroCcdToCcd(transaction.transaction.ccdCost) }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import { convertMicroCcdToCcd } from '~/utils/format'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/generated'

type Props = {
	transactions: Transaction[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
