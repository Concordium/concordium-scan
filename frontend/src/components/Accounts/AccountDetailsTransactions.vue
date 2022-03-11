<template>
	<div>
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
					v-for="accountTxRelation in transactions"
					:key="accountTxRelation.transaction.transactionHash"
				>
					<TableTd class="numerical">
						<StatusCircle
							:class="[
								'h-4 w-6 mr-2 text-theme-interactive',
								{
									'text-theme-error':
										accountTxRelation.transaction.result.__typename ===
										'Rejected',
								},
							]"
						/>

						<TransactionLink
							:id="accountTxRelation.transaction.id"
							:hash="accountTxRelation.transaction.transactionHash"
						/>
					</TableTd>
					<TableTd>
						<div class="whitespace-normal">
							{{
								translateTransactionType(
									accountTxRelation.transaction.transactionType
								)
							}}
						</div>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG" class="numerical">
						<AccountLink
							:address="accountTxRelation.transaction.senderAccountAddress"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.LG"
						align="right"
						class="numerical"
					>
						{{ convertMicroCcdToCcd(accountTxRelation.transaction.ccdCost) }}
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
import { convertMicroCcdToCcd } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import type { PageInfo, AccountTransactionRelation } from '~/types/generated'

const { breakpoint } = useBreakpoint()

type Props = {
	transactions: AccountTransactionRelation[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
