<template>
	<div>
		<AccountDetailsHeader :account="account" />
		<DrawerContent>
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
						:account-address="account.address.asString"
						:account-created-at="account.createdAt"
					/>
					<div v-else class="p-4">No entries</div>
				</template>
			</Accordion>
			<Accordion v-if="account.baker">
				Validator
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
					account.tokens?.nodes?.length && account.tokens?.nodes?.length > 0
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
			<Accordion
				v-if="account.plts?.nodes?.length && account.plts?.nodes?.length > 0"
			>
				Protocol-Level Tokens
				<template #content>
					<AccountDetailsPlts
						:go-to-page="goToPagePlt"
						:account-tokens="account.plts.nodes || []"
						:page-info="account.plts.pageInfo"
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
import AccountDetailsPlts from './AccountDetailsPlts.vue'
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
	goToPagePlt: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
