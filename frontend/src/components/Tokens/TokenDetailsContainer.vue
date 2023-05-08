<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />
	<TokenDetailsContent
		v-if="componentState === 'success' && data"
		:token="data.token"
		:metadata="metadata?.metadata"
		:metadata-url="data.token.metadataUrl"
		:go-to-page-account="goToPageAccount"
		:go-to-page-txn="goToPageTxn"
	/>
</template>
<script lang="ts" setup>
import { useTokenQuery } from '~/queries/useTokenQuery'
import { Scalars } from '~/types/generated'
import TokenDetailsContent from '~/components/Tokens/TokenDetailsContent.vue'
import { TokenMetadata } from '~/types/tokens'
import { fetchMetadata } from '~/utils/tokenUtils'

type Props = {
	tokenId: Scalars['String']
	contractIndex: Scalars['UnsignedLong']
	contractSubIndex: Scalars['UnsignedLong']
}
const props = defineProps<Props>()

const refTokenId = toRef(props, 'tokenId')
const refContractIndex = toRef(props, 'contractIndex')
const refContractSubIndex = toRef(props, 'contractSubIndex')
const {
	first: txnFirst,
	last: txnLast,
	after: txnAfter,
	before: txnBefore,
	goToPage: goToPageTxn,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const {
	first: accountFirst,
	last: accountLast,
	after: accountAfter,
	before: accountBefore,
	goToPage: goToPageAccount,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const { data, error, componentState } = useTokenQuery({
	tokenId: refTokenId,
	contractIndex: refContractIndex,
	contractSubIndex: refContractSubIndex,
	txnFirst,
	txnLast,
	txnAfter,
	txnBefore,
	accountFirst,
	accountLast,
	accountAfter,
	accountBefore,
})
const metadata = ref<{ metadata: TokenMetadata | undefined }>({
	metadata: undefined,
})
watchEffect(async () => {
	try {
		const metadataRes = await fetchMetadata(
			String(data.value?.token.metadataUrl)
		)
		metadata.value = { metadata: metadataRes }
	} catch {}
})
</script>
