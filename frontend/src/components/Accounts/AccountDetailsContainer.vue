<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<AccountDetailsContent
		v-else-if="componentState === 'success' && data"
		:account="data"
		:go-to-page-tx="goToPageTx"
		:go-to-page-release-schedule="goToPageReleaseSchedule"
		:go-to-page-account-statement="goToPageAccountStatement"
		:go-to-page-account-rewards="goToPageAccountRewards"
		:go-to-page-account-tokens="goToPageAccountTokens"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import { useAccountQuery } from '~/queries/useAccountQuery'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import AccountDetailsContent from '~/components/Accounts/AccountDetailsContent.vue'
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
	first: firstAccountReward,
	last: lastAccountReward,
	after: afterAccountReward,
	before: beforeAccountReward,
	goToPage: goToPageAccountRewards,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const {
	first: firstAccountToken,
	last: lastAccountToken,
	after: afterAccountToken,
	before: beforeAccountToken,
	goToPage: goToPageAccountTokens,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const {
	first: firstAccountStatement,
	last: lastAccountStatement,
	after: afterAccountStatement,
	before: beforeAccountStatement,
	goToPage: goToPageAccountStatement,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const transactionVariables = {
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
	firstAccountReward,
	lastAccountReward,
	afterAccountReward,
	beforeAccountReward,
	firstAccountToken,
	lastAccountToken,
	afterAccountToken,
	beforeAccountToken,
}

const { data, error, componentState } = useAccountQuery({
	id: refId as Ref<string>,
	address: refAddress as Ref<string>,
	transactionVariables,
})
</script>
