<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<SuspendedValidators
		v-else-if="componentState === 'success' && data"
		:passive-delegation-data="data"
		:go-to-page-delegators="goToPageDelegators"
		:go-to-page-rewards="goToPageRewards"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import { usePassiveDelegationQuery } from '~/queries/usePassiveDelegationQuery'
import SuspendedValidators from '~/components/SuspendedValidators/SuspendedValidatorsContent.vue'

const {
	first: firstDelegators,
	last: lastDelegators,
	after: afterDelegators,
	before: beforeDelegators,
	goToPage: goToPageDelegators,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })
const {
	first: firstRewards,
	last: lastRewards,
	after: afterRewards,
	before: beforeRewards,
	goToPage: goToPageRewards,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })
const pagingVariables = {
	firstDelegators,
	lastDelegators,
	afterDelegators,
	beforeDelegators,
	firstRewards,
	lastRewards,
	afterRewards,
	beforeRewards,
}
const { data, error, componentState } =
	usePassiveDelegationQuery(pagingVariables)
</script>
