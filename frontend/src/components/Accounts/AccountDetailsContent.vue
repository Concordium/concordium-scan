<template>
	<div>
		<AccountDetailsHeader :account="account" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Amount (Ͼ)</template>
					<template #default
						><span class="numerical">
							{{ convertMicroCcdToCcd(account.amount) }}
						</span>
					</template>
				</DetailsCard>
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
				<template #content>
					<AccountDetailsTransactions
						v-if="
							account.transactions?.nodes?.length &&
							account.transactions?.nodes?.length > 0
						"
						:transactions="account.transactions.nodes"
						:total-count="account.transactions!.nodes.length"
						:page-info="account!.transactions!.pageInfo"
						:go-to-page="goToPage"
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
						:go-to-page="goToPage"
					/>
					<div v-else class="p-4">No transactions</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import {
	formatTimestamp,
	convertTimestampToRelative,
	convertMicroCcdToCcd,
} from '~/utils/format'
import type { Account, PageInfo } from '~/types/generated'
import AccountDetailsHeader from '~/components/Accounts/AccountDetailsHeader.vue'
import AccountDetailsTransactions from '~/components/Accounts/AccountDetailsTransactions.vue'
import { useDateNow } from '~/composables/useDateNow'
import type { PaginationTarget } from '~/composables/usePagination'
import AccountDetailsReleaseScheduleTransactions from '~/components/Accounts/AccountDetailsReleaseScheduleTransactions.vue'

const { NOW } = useDateNow()

type Props = {
	account: Account
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
