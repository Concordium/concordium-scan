<template>
	<div v-if="accountQueryResult.data">
		<AccountDetailsContent
			v-if="accountQueryResult.data.account"
			:account="accountQueryResult.data.account"
			:go-to-page-tx="goToPageTx"
			:go-to-page-release-schedule="goToPageReleaseSchedule"
			:go-to-page-account-statement="goToPageAccountStatement"
		/>
		<AccountDetailsContent
			v-else
			:account="accountQueryResult.data.accountByAddress"
			:go-to-page-tx="goToPageTx"
			:go-to-page-release-schedule="goToPageReleaseSchedule"
			:go-to-page-account-statement="goToPageAccountStatement"
		/>
	</div>
	<BWCubeLogoIcon
		v-else
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import AccountDetailsContent from '~/components/Accounts/AccountDetailsContent.vue'
import {
	useAccountQuery,
	useAccountQueryByAddress,
} from '~/queries/useAccountQuery'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
type Props = {
	id?: string
	address?: string
}
const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refAddress = toRef(props, 'address')
const {
	first: firstTx,
	last: lastTx,
	after: afterTx,
	before: beforeTx,
	goToPage: goToPageTx,
} = usePagination()

const {
	first: firstReleaseSchedule,
	last: lastReleaseSchedule,
	after: afterReleaseSchedule,
	before: beforeReleaseSchedule,
	goToPage: goToPageReleaseSchedule,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const {
	first: firstAccountStatement,
	last: lastAccountStatement,
	after: afterAccountStatement,
	before: beforeAccountStatement,
	goToPage: goToPageAccountStatement,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const paginationVars = {
	firstTx,
	lastTx,
	afterTx,
	beforeTx,
	firstReleaseSchedule,
	lastReleaseSchedule,
	afterReleaseSchedule,
	beforeReleaseSchedule,
	firstAccountStatement,
	lastAccountStatement,
	afterAccountStatement,
	beforeAccountStatement,
}

const accountQueryResult = ref()
if (props.id)
	accountQueryResult.value = useAccountQuery(
		refId as Ref<string>,
		paginationVars
	)
else if (props.address)
	accountQueryResult.value = useAccountQueryByAddress(
		refAddress as Ref<string>,
		paginationVars
	)
</script>
