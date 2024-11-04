<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Token Address</TableTh>
					<TableTh>Contract</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Token Id</TableTh>
					<TableTh align="right">Balance</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="token in accountTokens"
					:key="token.token.tokenAddress"
				>
					<TableTd>
						<TokenLink
							:token-address="token.token.tokenAddress"
							:token-id="token.tokenId"
							:url="(token.token.metadataUrl as string)"
							:contract-address-index="token.contractIndex"
							:contract-address-sub-index="token.contractSubIndex"
						/>
					</TableTd>
					<TableTd>
						<ContractLink
							:address="token.token.contractAddressFormatted"
							:contract-address-index="token.contractIndex"
							:contract-address-sub-index="token.contractSubIndex"
						/>
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<TokenId :token-id="token.tokenId" />
					</TableTd>
					<TableTd align="right" class="numerical">
						<TokenAmount
							:symbol="token.metadata?.symbol"
							:amount="token.balance"
							:fraction-digits="token.metadata?.decimals || 0"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>
<script lang="ts" setup>
import TokenLink from '../molecules/TokenLink.vue'
import ContractLink from '../molecules/ContractLink.vue'
import TokenId from '../molecules/TokenId.vue'
import type { Account, AccountToken, PageInfo } from '~~/src/types/generated.js'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import TokenAmount from '~/components/atoms/TokenAmount.vue'
import { fetchMetadata } from '~~/src/types/tokens'

const { breakpoint } = useBreakpoint()

type Props = {
	accountTokens: (AccountToken & {
		metadata?: { decimals?: number; symbol?: string; name?: string }
	})[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	accountId: Account['id']
}
const props = defineProps<Props>()
watchEffect(() => {
	props.accountTokens
		.filter(t => t.token.metadataUrl)
		.forEach(async t => {
			try {
				t.metadata = await fetchMetadata(t.token.metadataUrl as string)
			} catch {}
		})
})
</script>
