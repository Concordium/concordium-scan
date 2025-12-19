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
						<div class="flex items-center gap-2 whitespace-normal">
							<span>
								{{
									translateTransactionType(
										accountTxRelation.transaction.transactionType
									)
								}}
							</span>
							<SponsorIcon
								v-if="accountTxRelation.transaction.sponsorAccountAddress"
								:glow-on="true"
								class="flex-shrink-0"
							/>
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
							:address="
								accountTxRelation.transaction.senderAccountAddress?.asString
							"
						/>
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.XXL"
						align="right"
						class="numerical"
					>
						<Amount :amount="accountTxRelation.transaction.ccdCost" />
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
import SponsorIcon from '~/components/icons/SponsorIcon.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import type { PageInfo, AccountTransactionRelation } from '~/types/generated'
import Amount from '~/components/atoms/Amount.vue'
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
