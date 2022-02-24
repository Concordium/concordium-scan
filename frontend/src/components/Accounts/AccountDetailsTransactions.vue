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
						<TransactionIcon class="h-4 w-4" />
						<LinkButton
							class="numerical"
							@click="
								drawer.push(
									'transaction',
									transaction.transaction.transactionHash,
									transaction.transaction.id
								)
							"
						>
							<Tooltip
								:text="transaction.transaction.transactionHash"
								text-class="text-theme-body"
							>
								{{ shortenHash(transaction.transaction.transactionHash) }}
							</Tooltip>
						</LinkButton>
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
		<Pagination
			v-if="pageInfo && totalCount > PAGE_SIZE"
			:page-info="pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import TransactionIcon from '~/components/icons/TransactionIcon.vue'
import { convertMicroCcdToCcd, shortenHash } from '~/utils/format'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Transaction } from '~/types/transactions'
import type { PageInfo } from '~/types/generated'
import { useDrawer } from '~/composables/useDrawer'

type Props = {
	transactions: Transaction[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
const drawer = useDrawer()
defineProps<Props>()
</script>
