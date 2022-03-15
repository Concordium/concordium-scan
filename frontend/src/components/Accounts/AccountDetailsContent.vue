<template>
	<div>
		<AccountDetailsHeader :account="account" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Created</template>
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
						v-if="account.transactions?.nodes?.length"
						:transactions="account.transactions.nodes"
						:total-count="account.transactions!.nodes.length"
						:page-info="account!.transactions!.pageInfo"
						:go-to-page="goToPage"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import type { Account, PageInfo } from '~/types/generated'
import AccountDetailsHeader from '~/components/Accounts/AccountDetailsHeader.vue'
import AccountDetailsTransactions from '~/components/Accounts/AccountDetailsTransactions.vue'
import { useDateNow } from '~/composables/useDateNow'
import type { PaginationTarget } from '~/composables/usePagination'

const { NOW } = useDateNow()

type Props = {
	account: Account
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
