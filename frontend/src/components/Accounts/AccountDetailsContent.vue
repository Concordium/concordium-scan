<template>
	<div>
		<AccountDetailsHeader :account="account" />
		<DrawerContent>
			<Alert v-if="account.delegation && account.delegation?.pendingChange">
				Pending change
				<template
					v-if="
						account.delegation?.pendingChange?.__typename ===
						'PendingDelegationReduceStake'
					"
					#secondary
				>
					<!-- vue-tsc doesn't seem to be satisfied with the template condition ... -->
					<span
						v-if="
							account.delegation?.pendingChange?.__typename ===
							'PendingDelegationReduceStake'
						"
					>
						Stake will be reduced to
						<Amount
							:amount="account.delegation?.pendingChange?.newStakedAmount"
							:show-symbol="true"
						/>
						in
						<Tooltip
							:text="
								formatTimestamp(
									account.delegation?.pendingChange?.effectiveTime
								)
							"
						>
							{{
								convertTimestampToRelative(
									account.delegation?.pendingChange?.effectiveTime,
									NOW
								)
							}}
						</Tooltip>
					</span>
				</template>
				<template
					v-else-if="
						account.delegation?.pendingChange?.__typename ===
						'PendingDelegationRemoval'
					"
					#secondary
				>
					Delegation will be removed in
					<Tooltip
						:text="
							formatTimestamp(account.delegation?.pendingChange?.effectiveTime)
						"
					>
						{{
							convertTimestampToRelative(
								account.delegation?.pendingChange?.effectiveTime,
								NOW
							)
						}}
					</Tooltip>
				</template>
			</Alert>
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<AccountDetailsAmounts :account="account" />
				<DetailsCard>
					<template #title>Age</template>
					<template #default>
						{{ convertTimestampToRelative(account.createdAt, NOW) }}
					</template>
					<template #secondary>
						{{ formatTimestamp(account.createdAt) }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Transactions
				<span class="numerical text-theme-faded"
					>({{ account.transactionCount }})</span
				>
				<template #content>
					<AccountDetailsTransactions
						v-if="
							account.transactions?.nodes?.length &&
							account.transactions?.nodes?.length > 0
						"
						:transactions="account.transactions.nodes"
						:total-count="account.transactions!.nodes.length"
						:page-info="account!.transactions!.pageInfo"
						:go-to-page="goToPageTx"
					/>
					<div v-else class="p-4">No transactions</div>
				</template>
			</Accordion>
			<Accordion v-if="account.releaseSchedule.totalAmount > 0">
				<span class="">Release schedule</span>
				<template #content>
					<div class="px-4 pb-4">
						Total amount locked Ͼ
						<span class="numerical inline-block">{{
							convertMicroCcdToCcd(account.releaseSchedule.totalAmount)
						}}</span>
					</div>
					<AccountDetailsReleaseScheduleTransactions
						v-if="account.releaseSchedule.schedule?.nodes?.length"
						:release-schedule-items="account.releaseSchedule.schedule.nodes"
						:page-info="account.releaseSchedule.schedule.pageInfo"
						:go-to-page="goToPageReleaseSchedule"
					/>
					<div v-else class="p-4">No transactions</div>
				</template>
			</Accordion>
			<Accordion>
				Account statement
				<template #content>
					<AccountDetailsAccountStatement
						v-if="
							account.accountStatement?.nodes?.length &&
							account.accountStatement?.nodes?.length > 0
						"
						:account-statement-items="account.accountStatement.nodes"
						:page-info="account.accountStatement.pageInfo"
						:go-to-page="goToPageAccountStatement"
						:export-url="exportUrl(account.address.asString)"
					/>
					<div v-else class="p-4">No entries</div>
				</template>
			</Accordion>
			<Accordion v-if="account.baker">
				Baker
				<template #content>
					<AccountDetailsBaker :baker="account.baker" />
				</template>
			</Accordion>
			<Accordion v-if="account.delegation">
				Delegation
				<template #content>
					<AccountDetailsDelegation :delegation="account.delegation" />
				</template>
			</Accordion>
			<Accordion
				v-if="
					account.rewards?.nodes?.length && account.rewards?.nodes.length > 0
				"
			>
				Rewards
				<template #content>
					<AccountDetailsRewards
						:account-rewards="account.rewards.nodes"
						:page-info="account.rewards.pageInfo"
						:go-to-page="goToPageAccountRewards"
						:account-id="account.id"
					/>
				</template>
			</Accordion>
			<Accordion
				v-if="
					account.tokens?.pageInfo.hasNextPage ||
					account.tokens?.pageInfo.hasPreviousPage
				"
			>
				Tokens
				<template #content>
					<AccountDetailsToken
						:account-tokens="account.tokens.nodes || []"
						:page-info="account.tokens.pageInfo"
						:go-to-page="goToPageAccountTokens"
						:account-id="account.id"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import AccountDetailsBaker from './AccountDetailsBaker.vue'
import AccountDetailsHeader from './AccountDetailsHeader.vue'
import AccountDetailsAmounts from './AccountDetailsAmounts.vue'
import AccountDetailsTransactions from './AccountDetailsTransactions.vue'
import AccountDetailsToken from './AccountDetailsToken.vue'
import AccountDetailsReleaseScheduleTransactions from './AccountDetailsReleaseScheduleTransactions.vue'
import AccountDetailsAccountStatement from './AccountDetailsAccountStatement.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import {
	formatTimestamp,
	convertTimestampToRelative,
	convertMicroCcdToCcd,
} from '~/utils/format'
import type { Account, PageInfo } from '~/types/generated'
import { useDateNow } from '~/composables/useDateNow'
import type { PaginationTarget } from '~/composables/usePagination'
import AccountDetailsDelegation from '~/components/Accounts/AccountDetailsDelegation.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Amount from '~/components/atoms/Amount.vue'
import AccountDetailsRewards from '~/components/Accounts/AccountDetailsRewards.vue'

const { NOW } = useDateNow()

type Props = {
	account: Account
	goToPageTx: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageReleaseSchedule: (
		page: PageInfo
	) => (target: PaginationTarget) => void
	goToPageAccountStatement: (
		page: PageInfo
	) => (target: PaginationTarget) => void
	goToPageAccountRewards: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageAccountTokens: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()

const { apiUrl } = useRuntimeConfig()

function exportUrl(accountAddress: string) {
	const url = new URL(apiUrl)
	url.pathname = 'rest/export/statement' // setting pathname discards any existing path in 'apiUrl'
	url.searchParams.append('accountAddress', accountAddress)
	return url.toString()
}
</script>
