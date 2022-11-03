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
					:key="accountStatementItem.id"
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
						<EncryptedIcon
							v-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.AmountEncrypted
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<DecryptedIcon
							v-else-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.AmountDecrypted
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<TransferIconIn
							v-else-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.TransferIn
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<TransferIconOut
							v-else-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.TransferOut
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<RewardIcon
							v-else-if="
								accountStatementItem.entryType ===
									AccountStatementEntryType.FinalizationReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.TransactionFeeReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.BakerReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.FoundationReward
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<FeeIcon
							v-else-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.TransactionFee
							"
							class="h-4 text-theme-white inline align-text-top"
						/>
						<Tooltip
							v-if="
								accountStatementItem.entryType ===
									AccountStatementEntryType.FinalizationReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.TransactionFeeReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.BakerReward ||
								accountStatementItem.entryType ===
									AccountStatementEntryType.FoundationReward
							"
							:text="translateBakerRewardType(accountStatementItem.entryType)"
						>
							<span v-if="breakpoint >= Breakpoint.LG" class="pl-2">{{
								translateAccountStatementEntryType(
									accountStatementItem.entryType
								)
							}}</span></Tooltip
						>

						<span v-else-if="breakpoint >= Breakpoint.LG" class="pl-2">{{
							translateAccountStatementEntryType(accountStatementItem.entryType)
						}}</span>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XL"
						><TransactionLink
							v-if="accountStatementItem.reference.__typename === 'Transaction'"
							:hash="accountStatementItem.reference.transactionHash"
						></TransactionLink>
						<BlockLink
							v-if="accountStatementItem.reference.__typename === 'Block'"
							:hash="accountStatementItem.reference.blockHash"
						></BlockLink>
					</TableTd>
					<TableTd align="right" class="numerical">
						<Amount :amount="accountStatementItem.amount" />
					</TableTd>
					<TableTd
						v-if="breakpoint >= Breakpoint.XXL"
						align="right"
						class="numerical"
					>
						<Amount :amount="accountStatementItem.accountBalance" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<div class="grid grid-cols-4 mt-8">
			<div class="col-span-3">
				<Pagination
					v-if="pageInfo && (pageInfo.hasNextPage || pageInfo.hasPreviousPage)"
					:page-info="pageInfo"
					:go-to-page="goToPage"
				/>
			</div>
			<div class="col-span-1 flex justify-end">
				<a
					class="bg-theme-button-primary px-8 py-3 hover:bg-theme-button-primary-hover rounded"
					:href="exportUrl"
				>
					<span class="hidden md:inline">Export</span>
					<DownloadIcon class="h-4 inline align-text-top" />
				</a>
			</div>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { DownloadIcon } from '@heroicons/vue/solid/index.js'
import Amount from '~/components/atoms/Amount.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import RewardIcon from '~/components/icons/RewardIcon.vue'
import FeeIcon from '~/components/icons/FeeIcon.vue'
import TransferIconIn from '~/components/icons/TransferIconIn.vue'
import TransferIconOut from '~/components/icons/TransferIconOut.vue'
import EncryptedIcon from '~/components/icons/EncryptedIcon.vue'
import DecryptedIcon from '~/components/icons/DecryptedIcon.vue'
import { translateBakerRewardType } from '~/utils/translateBakerRewardType'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import { translateAccountStatementEntryType } from '~/utils/translateAccountStatementEntry'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import { useDateNow } from '~/composables/useDateNow'
import {
	type PageInfo,
	type AccountStatementEntry,
	AccountStatementEntryType,
} from '~/types/generated'

const { NOW } = useDateNow()
const { breakpoint } = useBreakpoint()

type Props = {
	accountStatementItems: AccountStatementEntry[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	exportUrl: string
}
defineProps<Props>()
</script>
