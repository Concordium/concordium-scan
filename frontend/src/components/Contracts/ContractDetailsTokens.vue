<template>
	<TableHead>
		<TableRow>
			<TableTh>Token Address</TableTh>
			<TableTh>Token Id</TableTh>
			<TableTh align="right"> Supply </TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="contractToken in tokensWithMetadata" :key="contractToken">
			<TableTd>
				<TokenLink
					:token-address="contractToken.tokenAddress"
					:token-id="contractToken.tokenId"
					:url="(contractToken.metadataUrl as string)"
					:contract-address-index="contractToken.contractIndex"
					:contract-address-sub-index="contractToken.contractSubIndex"
				/>
			</TableTd>
			<TableTd>
				<TokenId :token-id="contractToken.tokenId" />
			</TableTd>
			<TableTd align="right" class="numerical">
				<TokenAmount
					:symbol="contractToken.metadata?.symbol"
					:amount="String(contractToken.totalSupply)"
					:fraction-digits="Number(contractToken.metadata?.decimals || 0)"
				/>
			</TableTd>
		</TableRow>
	</TableBody>
</template>
<script lang="ts" setup>
import { ComputedRef } from 'vue'
import TokenLink from '../molecules/TokenLink.vue'
import TokenId from '../molecules/TokenId.vue'
import TokenAmount from '../atoms/TokenAmount.vue'
import { Token } from '~~/src/types/generated'
import { TokenWithMetadata, fetchMetadata } from '~~/src/types/tokens'

type Props = {
	contractTokens: Token[]
}
const props = defineProps<Props>()

const tokensWithMetadata: ComputedRef<TokenWithMetadata[]> = computed(
	() => props.contractTokens || []
)

watchEffect(() => {
	tokensWithMetadata.value
		.filter(t => t.metadataUrl)
		.forEach(async t => {
			try {
				t.metadata = await fetchMetadata(String(t.metadataUrl))
			} catch {}
		})
})
</script>
