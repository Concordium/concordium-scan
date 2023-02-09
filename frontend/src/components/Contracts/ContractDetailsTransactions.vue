<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Hash</TableTh>
					<TableTh>Type</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Age</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XXL">Sender</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XXL" align="right">
						Cost (Ͼ)
					</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="txnRelation in transactions"
					:key="txnRelation.transaction.transactionHash"
				>
					<TableTd class="numerical">
						<TransactionResult :result="txnRelation.transaction.result" />
						<TransactionLink
							:id="txnRelation.transaction.id"
							:hash="txnRelation.transaction.transactionHash"
						/>
					</TableTd>
					<TableTd>
						<div class="whitespace-normal">
							{{
								translateTransactionType(
									txnRelation.transaction.transactionType
								)
							}}
						</div>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<Tooltip
							:text="
								formatTimestamp(txnRelation.transaction.block.blockSlotTime)
							"
						>
							{{
								convertTimestampToRelative(
									txnRelation.transaction.block.blockSlotTime,
									NOW
								)
							}}
						</Tooltip>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XXL" class="numerical">
						<AccountLink
							:address="txnRelation.transaction.senderAccountAddress?.asString"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.XXL"
						align="right"
						class="numerical"
					>
						<Amount :amount="txnRelation.transaction.ccdCost" />
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
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, ContractTransactionRelation } from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'
import TransactionResult from '~/components/molecules/TransactionResult.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	transactions: ContractTransactionRelation[]
	pageInfo: PageInfo
	totalCount: number
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
