<template>
	<div>
		<Title>CCDScan | Tokens</Title>

		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Token Id</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Contract</TableTh>
					<TableTh v-if="breakpoint >= Breakpoint.SM">{{
						breakpoint >= Breakpoint.LG ? 'Metadata Url' : 'Metadata'
					}}</TableTh>
					<TableTh align="right"> Supply </TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="token in tokens"
					:key="token.contractIndex + token.tokenId"
				>
					<TableTd>
						<TokenLink :data="token.tokenId" :url="token.metadataUrl" />
					</TableTd>
					<TableTd>
						{{ token.contractIndex }} / {{ token.contractSubIndex }}
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.LG">
						<TokenLink
							:data="token.metadataUrl"
							:url="token.metadataUrl"
							:length="25"
							:suffix="'...'"
						/>
					</TableTd>

					<TableTd v-if="breakpoint >= Breakpoint.SM" align="right">
						<span class="numerical">
							<TokenAmount
								:symbol="token.metadata?.symbol"
								:amount="token.totalSupply"
								:fraction-digits="Number(token.metadata?.decimals || 0)"
							/>
						</span>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>

		<Pagination
			v-if="data?.tokens.pageInfo"
			:page-info="data?.tokens.pageInfo"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { ComputedRef } from 'vue'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'
import { usePagination } from '~/composables/usePagination'
import Pagination from '~/components/Pagination.vue'
import { useTokensListQuery } from '~/queries/useTokensListQuery'
import { Token } from '~/types/generated'
import TokenAmount from '~~/src/components/atoms/TokenAmount.vue'
import { fetchMetadata } from '~/utils/tokenUtils'

const { breakpoint } = useBreakpoint()
const { first, last, after, before, goToPage } = usePagination()
const { data } = useTokensListQuery({
	first,
	last,
	after,
	before,
})
// eslint-disable-next-line @typescript-eslint/no-explicit-any
const tokens: ComputedRef<(Token & { metadata?: any })[]> = computed(
	() => data.value?.tokens.nodes || []
)
watchEffect(() => {
	tokens.value
		.filter(t => t.metadataUrl)
		.forEach(async t => {
			try {
				t.metadata = await fetchMetadata(String(t.metadataUrl))
			} catch {}
		})
})
</script>
