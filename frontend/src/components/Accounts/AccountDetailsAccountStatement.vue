<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Time</TableTh>
					<TableTh>Type</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XL">Reference</TableTh>
					<TableTh align="right">Amount</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.XXL" align="right"
						>Balance</TableTh
					>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="accountStatementItem in accountStatementItems"
					:key="
						accountStatementItem.timestamp +
						'-' +
						accountStatementItem.entryType +
						'-' +
						accountStatementItem.amount
					"
				>
					<TableTd>
						<Tooltip
							:text="
								convertTimestampToRelative(accountStatementItem.timestamp, NOW)
							"
						>
							{{ formatTimestamp(accountStatementItem.timestamp) }}
						</Tooltip>
					</TableTd>
					<TableTd>
						<TransferIconIn
							v-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.TransferIn
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<TransferIconOut
							v-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.TransferOut
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<RewardIcon
							v-if="
								accountStatementItem.entryType ===
									AccountStatementEntryType.BakingReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.BlockReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.MintReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.FinalizationReward
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<FeeIcon
							v-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.TransactionFee
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<span v-if="breakpoint >= Breakpoint.LG">{{
							translateAccountStatementEntryType(accountStatementItem.entryType)
						}}</span>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XL"
						><TransactionLink
							v-if="accountStatementItem.reference.transactionHash"
							:hash="accountStatementItem.reference.transactionHash"
						></TransactionLink>
						<BlockLink
							v-if="accountStatementItem.reference.blockHash"
							:hash="accountStatementItem.reference.blockHash"
						></BlockLink>
					</TableTd>
					<TableTd align="right" class="numerical">
						{{ convertMicroCcdToCcd(accountStatementItem.amount) }}
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.XXL"
						align="right"
						class="numerical"
					>
						{{ convertMicroCcdToCcd(accountStatementItem.accountBalance) }}
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
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import {
	type PageInfo,
	type AccountStatementEntry,
	AccountStatementEntryType,
} from '~/types/generated'
import { translateAccountStatementEntryType } from '~/utils/translateAccountStatementEntry'
import RewardIcon from '~/components/icons/RewardIcon.vue'
import FeeIcon from '~/components/icons/FeeIcon.vue'
import TransferIconIn from '~/components/icons/TransferIconIn.vue'
import TransferIconOut from '~/components/icons/TransferIconOut.vue'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	accountStatementItems: AccountStatementEntry[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
