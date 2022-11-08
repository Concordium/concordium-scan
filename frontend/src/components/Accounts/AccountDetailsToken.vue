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
							:url="token.token.metadataUrl"
						/>
					</TableTd>
					<TableTd>
						{{ token.balance }}
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

const { breakpoint } = useBreakpoint()

type Props = {
	accountTokens: AccountToken[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	accountId: Account['id']
}
const props = defineProps<Props>()
</script>
