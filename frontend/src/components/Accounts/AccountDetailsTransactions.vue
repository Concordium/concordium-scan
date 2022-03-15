<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Hash</TableTh>
					<TableTh>Type</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Timestamp</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XXL">Sender</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XXL" align="right">
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
						<TransactionResult :result="accountTxRelation.transaction.result" />
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
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<Tooltip
							:text="
								formatTimestamp(
									accountTxRelation.transaction.block.blockSlotTime
								)
							"
						>
							{{
								convertTimestampToRelative(
									accountTxRelation.transaction.block.blockSlotTime,
									NOW
								)
							}}
						</Tooltip>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XXL" class="numerical">
						<AccountLink
							:address="accountTxRelation.transaction.senderAccountAddress"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.XXL"
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
import Tooltip from '~/components/atoms/Tooltip.vue'
import {
	convertMicroCcdToCcd,
	formatTimestamp,
	convertTimestampToRelative,
} from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, AccountTransactionRelation } from '~/types/generated'
import TransactionResult from '~/components/molecules/TransactionResult.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	transactions: AccountTransactionRelation[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
