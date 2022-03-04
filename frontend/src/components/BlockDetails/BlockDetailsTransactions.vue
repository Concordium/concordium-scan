<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Transaction hash</TableTh>
					<TableTh>Sender</TableTh>
					<TableTh align="right">Cost (Ͼ)</TableTh>
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
					<TableTd class="numerical">
						<AccountLink :address="transaction.senderAccountAddress" />
					</TableTd>
					<TableTd align="right" class="numerical">
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
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/generated'

type Props = {
	transactions: Transaction[]
	totalCount: number
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
