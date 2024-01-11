<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />
	<TokenDetailsContent
		v-else-if="componentState === 'success' && data?.token"
		:token="data?.token"
		:pagination-events="pageOffsetInfoEvents"
		:pagination-accounts="pageOffsetInfoAccount"
		:page-dropdown-events="pageDropdownEvents"
		:page-dropdown-accounts="pageDropdownAccounts"
		:fetching="fetching"
	/>
</template>
<script lang="ts" setup>
import TokenDetailsContent from './TokenDetailsContent.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { usePaginationOffset } from '~~/src/composables/usePaginationOffset'
import { usePageDropdown } from '~~/src/composables/usePageDropdown'
import { useTokenQuery } from '~~/src/queries/useTokenQuery'

type Props = {
	tokenId: string
	contractAddressIndex: number
	contractAddressSubIndex: number
}
const props = defineProps<Props>()

const pageDropdownEvents = usePageDropdown()
const pageDropdownAccounts = usePageDropdown()

const pageOffsetInfoEvents = usePaginationOffset(pageDropdownEvents.take)
const pageOffsetInfoAccount = usePaginationOffset(pageDropdownAccounts.take)

const tokenId = ref(props.tokenId)
const contractAddressIndex = ref(props.contractAddressIndex)
const contractAddressSubIndex = ref(props.contractAddressSubIndex)

const { data, error, componentState, fetching } = useTokenQuery({
	tokenId,
	contractAddressIndex,
	contractAddressSubIndex,
	eventsVariables: {
		skip: pageOffsetInfoEvents.skip,
		take: pageOffsetInfoEvents.take,
	},
	accountsVariables: {
		skip: pageOffsetInfoAccount.skip,
		take: pageOffsetInfoAccount.take,
	},
})
</script>
