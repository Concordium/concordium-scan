<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Index</TableTh>
					<TableTh>Token Id</TableTh>
					<TableTh>Balance</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="token in accountTokens"
					:key="token.contractIndex + token.contractSubIndex + token.tokenId"
				>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						{{ token.contractIndex }} / {{ token.contractSubIndex }}
					</TableTd>
					<TableTd>
						<TokenLink
							:token-id="token.tokenId"
							:contract-index="token.contractIndex"
							:contract-sub-index="token.contractSubIndex"
							:url="token.token.metadataUrl as string"
						/>
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
import { Account, AccountToken, PageInfo } from '~~/src/types/generated.js'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'
import TokenAmount from '~/components/atoms/TokenAmount.vue'
import { fetchMetadata } from '~~/src/utils/tokenUtils'
import TokenLink from '~/components/molecules/TokenLink.vue'

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
				const json = await fetchMetadata(t.token.metadataUrl as string)
				t.metadata = json
			} catch {}
		})
})
</script>
