<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Name</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Plt Id</TableTh>
					<TableTh align="right">Balance</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="plt in accountTokens" :key="plt.tokenId">
					<TableTd>
						{{ plt.tokenName }}
					</TableTd>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<TokenId :token-id="plt.tokenId" />
					</TableTd>
					<TableTd align="right" class="numerical">
						<PltAmount :value="plt.amount.toString()" :decimals="plt.decimal" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>
<script lang="ts" setup>
import type { PltByAccountAddress, PageInfo } from '~~/src/types/generated.js'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import type { PaginationTarget } from '~/composables/usePagination'

const { breakpoint } = useBreakpoint()

type Props = {
	accountTokens: PltByAccountAddress[]
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
}
defineProps<Props>()
</script>
