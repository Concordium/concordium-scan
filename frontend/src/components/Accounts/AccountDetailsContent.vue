<template>
	<div>
		<AccountDetailsHeader v-if="props.account" :account="props.account" />
		<DrawerContent v-if="props.account">
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard v-if="props.account?.createdAt">
					<template #title>Created At</template>
					<template #default>
						{{ convertTimestampToRelative(props.account?.createdAt, NOW) }}
					</template>
					<template #secondary>
						{{ props.account?.createdAt }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Transactions
				<template #content>
					<AccountDetailsTransactions
						:transactions="props.account?.transactions?.nodes"
						:total-count="props.account?.transactions?.nodes.length"
						:page-info="props.account?.transactions?.pageInfo"
						:go-to-page="props.goToPage"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { convertTimestampToRelative } from '~/utils/format'
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

const props = defineProps<Props>()
</script>
