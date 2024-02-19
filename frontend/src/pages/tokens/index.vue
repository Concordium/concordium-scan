<template>
	<div>
		<Title>CCDScan | CIS-2 Tokens</Title>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh> Address </TableTh>
					<TableTh> Contract </TableTh>
					<TableTh> Id </TableTh>
					<TableTh align="right"> Supply </TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="token in tokens"
					:key="token.contractIndex + token.contractSubIndex + token.tokenId"
				>
					<TableTd>
						<TokenLink
							:token-address="token.tokenAddress"
							:token-id="token.tokenId"
							:contract-address-index="token.contractIndex"
							:contract-address-sub-index="token.contractSubIndex"
						/>
					</TableTd>
					<TableTd>
						<ContractLink
							:address="token.contractAddressFormatted"
							:contract-address-index="token.contractIndex"
							:contract-address-sub-index="token.contractSubIndex"
						/>
					</TableTd>
					<TableTd>
						<TokenId :token-id="token.tokenId" />
					</TableTd>
					<TableTd align="right">
						<TokenAmount
							:symbol="token.metadata?.symbol"
							:amount="token.totalSupply"
							:fraction-digits="Number(token.metadata?.decimals || 0)"
						/>
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
import Pagination from '~/components/Pagination.vue'
import Table from '~~/src/components/Table/Table.vue'
import TableBody from '~~/src/components/Table/TableBody.vue'
import TableHead from '~~/src/components/Table/TableHead.vue'
import TableRow from '~~/src/components/Table/TableRow.vue'
import TableTd from '~~/src/components/Table/TableTd.vue'
import TableTh from '~~/src/components/Table/TableTh.vue'
import TokenLink from '~~/src/components/molecules/TokenLink.vue'
import { useTokensListQuery } from '~~/src/queries/useTokensListQuery'
import { TokenWithMetadata, fetchMetadata } from '~~/src/types/tokens'
import ContractLink from '~~/src/components/molecules/ContractLink.vue'
import TokenAmount from '~~/src/components/atoms/TokenAmount.vue'
import TokenId from '~~/src/components/molecules/TokenId.vue'

const { first, last, after, before, goToPage } = usePagination()
const { data } = useTokensListQuery({
	first,
	last,
	after,
	before,
})

const tokens: ComputedRef<TokenWithMetadata[]> = computed(
	() => (data.value?.tokens.nodes as TokenWithMetadata[]) || []
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
