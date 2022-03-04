<template>
	<div>
		<AccountDetailsHeader v-if="props.account" :account="props.account" />
		<DrawerContent v-if="props.account">
			<div class="grid gap-6 grid-cols-2 mb-16">
				<DetailsCard v-if="props.account?.createdAt">
					<template #title>Created At</template>
					<template #default>
						{{ convertTimestampToRelative(props.account?.createdAt) }}
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
import type { PaginationTarget } from '~/composables/usePagination'
type Props = {
	account: Account
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}

const props = defineProps<Props>()
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}
.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
