<template>
	<div>
		<TokenDetailsHeader :token="token" :metadata-url="metadataUrl" />
		<DrawerContent>
			<div class="grid md:grid-cols-2 mb-4">
				<TokenDetailsAmounts :token="token" :metadata="metadata" />
				<DetailsCard>
					<template #title>Age</template>
					<template #default>
						{{
							convertTimestampToRelative(
								token.createTransaction.block.blockSlotTime,
								NOW
							)
						}}
					</template>
					<template #secondary>
						{{ formatTimestamp(NOW.toISOString()) }}
					</template>
				</DetailsCard>
			</div>
		</DrawerContent>
		<DrawerContent v-if="metadata">
			<TokenDetailsMetadataContent
				:metadata="metadata"
				:metadata-url="metadataUrl"
			/>
		</DrawerContent>
		<DrawerContent>
			<Accordion>
				Transactions
				<template #content>
					<TokenDetailsTransactions
						v-if="
							token.transactions?.nodes?.length &&
							token.transactions?.nodes?.length > 0
						"
						:transactions="token.transactions.nodes"
						:total-count="token.transactions!.nodes.length"
						:page-info="token!.transactions!.pageInfo"
						:go-to-page="goToPageTxn"
					/>
					<div v-else class="p-4">No transactions</div>
				</template>
			</Accordion>
			<Accordion v-if="token.accounts?.nodes?.length">
				Accounts
				<template #content>
					<TokenDetailsAccounts
						v-if="
							token.accounts?.nodes?.length && token.accounts?.nodes?.length > 0
						"
						:accounts="token.accounts.nodes"
						:metadata="metadata"
						:page-info="token!.accounts!.pageInfo"
						:go-to-page="goToPageTxn"
					/>
					<div v-else class="p-4">No Accounts</div>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import TokenDetailsHeader from './TokenDetailsHeader.vue'
import TokenDetailsAmounts from './TokenDetailsAmounts.vue'
import TokenDetailsTransactions from './TokenDetailsTransactions.vue'
import TokenDetailsAccounts from './TokenDetailsAccounts.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import type { PageInfo, Token } from '~/types/generated'
import type { PaginationTarget } from '~/composables/usePagination'
import { TokenMetadata } from '~/types/tokens'
import DetailsCard from '~/components/DetailsCard.vue'
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import TokenDetailsMetadataContent from '~/components/Tokens/TokenDetailsMetadataContent.vue'

const { NOW } = useDateNow()

type Props = {
	token: Token
	metadata?: TokenMetadata
	metadataUrl?: string | null
	goToPageTxn: (page: PageInfo) => (target: PaginationTarget) => void
	goToPageAccount: (page: PageInfo) => (target: PaginationTarget) => void
}

defineProps<Props>()
</script>
