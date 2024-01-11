<template>
	<div>Some context</div>
</template>
<script lang="ts" setup>
import { PageDropdownInfo } from '~~/src/composables/usePageDropdown'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'
import { Token } from '~~/src/types/generated'

type Props = {
	token: Token
	paginationEvents: PaginationOffsetInfo
	paginationAccounts: PaginationOffsetInfo
	pageDropdownEvents: PageDropdownInfo
	pageDropdownAccounts: PageDropdownInfo
	fetching: boolean
}
const props = defineProps<Props>()

const tabList = computed(() => {
	return [
		`Event (${props.token.tokenEvents?.totalCount ?? 0})`,
		`Accounts (${props.token.contractRejectEvents?.totalCount ?? 0})`,
	]
})
</script>
