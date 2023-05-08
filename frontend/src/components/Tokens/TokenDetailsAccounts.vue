<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Address</TableTh>
					<TableTh align="right">Balance</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="account in accounts"
					:key="account.account?.address.asString"
				>
					<TableTd>
						<AccountLink :address="account.account?.address.asString" />
					</TableTd>
					<TableTd align="right" class="numerical">
						<TokenAmount
							:symbol="metadata?.symbol"
							:amount="account.balance"
							:fraction-digits="metadata?.decimals || 0"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>
<script lang="ts" setup>
import AccountLink from '../molecules/AccountLink.vue'
import { AccountToken, PageInfo } from '~/types/generated.js'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import TokenAmount from '~/components/atoms/TokenAmount.vue'
import { TokenMetadata } from '~/types/tokens'

const { breakpoint } = useBreakpoint()

type Props = {
	accounts: AccountToken[]
	metadata?: TokenMetadata
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
const props = defineProps<Props>()
</script>
