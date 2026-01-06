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
						<SponsorIcon
							v-else-if="
								accountStatementItem.entryType ===
								AccountStatementEntryType.SponsoredTransactionFee
							"
							:glow-on="false"
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
							}}</span>
						</Tooltip>

						<span v-else-if="breakpoint >= Breakpoint.LG" class="pl-2">{{
							translateAccountStatementEntryType(accountStatementItem.entryType)
						}}</span>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.XL">
						<TransactionLink
							v-if="accountStatementItem.reference.__typename === 'Transaction'"
							:hash="accountStatementItem.reference.transactionHash"
						/>
						<BlockLink
							v-if="accountStatementItem.reference.__typename === 'Block'"
							:hash="accountStatementItem.reference.blockHash"
						/>
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
					:href="exportUrl(accountAddress, chosenMonth!)"
				>
					<span class="hidden md:inline">Export</span>
					<DownloadIcon class="h-4 inline align-text-top" />
				</a>
			</div>
		</div>
		<div class="col-span-1 flex mt-3 flex justify-end">
			<month-picker-input
				variant="dark"
				class="py-2"
				type="month"
				:min-date="createdAtMonth"
				:max-date="currentMonth"
				:default-month="currentMonth.getMonth() + 1"
				@change="
					(update: any) => {
						chosenMonth = buildMonthInput(update.year, update.monthIndex)
					}
				"
			/>
		</div>
	</div>
</template>

<script lang="ts" setup>
// @ts-expect-error No tyoe definitions for 'vue-month-picker' library.
import { MonthPickerInput } from 'vue-month-picker'
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

/**
 * @input month the number of the month, 1-indexed i.e. January = 1
 */
function buildMonthInput(year: number, month: number) {
	const monthString = ('0' + month).slice(-2)
	return `${year}-${monthString}`
}

/// Takes a date and returns the corresponding "YYYY-MM" string.
/// if no date is given, undefined is returned.
function toMonthInput(date?: Date): string | undefined {
	if (date) {
		return buildMonthInput(date.getFullYear(), date.getMonth() + 1)
	}
	return undefined
}

type Props = {
	accountStatementItems: AccountStatementEntry[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	accountAddress: string
	accountCreatedAt: string
}
const props = defineProps<Props>()

const createdAtMonth = new Date(props.accountCreatedAt)
// Set createdAtMonth date to first in the month, because otherwise the Month picker will not include that month.
createdAtMonth.setUTCDate(0)
const currentMonth = NOW.value
const chosenMonth = ref(toMonthInput(currentMonth))

const {
	public: { apiUrl },
} = useRuntimeConfig()

function exportUrl(accountAddress: string, rawMonth: string) {
	const url = new URL(apiUrl)
	url.pathname = 'rest/export/statement' // setting pathname discards any existing path in 'apiUrl'
	url.searchParams.append('accountAddress', accountAddress)
	if (rawMonth && !isNaN(new Date(rawMonth).getTime())) {
		const start = new Date(rawMonth)
		const end = new Date(rawMonth)
		end.setMonth(end.getMonth() + 1)
		url.searchParams.append('fromTime', start.toISOString())
		url.searchParams.append('toTime', end.toISOString())
	}
	return url.toString()
}
</script>
