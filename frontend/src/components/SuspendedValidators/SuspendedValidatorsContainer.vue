<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<SuspendedValidators
		v-else-if="componentState === 'success' && data"
		:data="data"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import { useSuspendedValidatorsQuery } from '~/queries/useSuspendedValidatorsQuery'
import SuspendedValidators from '~/components/SuspendedValidators/SuspendedValidatorsContent.vue'

const {
	first: firstSuspendedValidators,
	last: lastSuspendedValidators,
	after: afterSuspendedValidators,
	before: beforeSuspendedValidators,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })
const {
	first: firstPrimedForSuspensionValidators,
	last: lastPrimedForSuspensionValidators,
	after: afterPrimedForSuspensionValidators,
	before: beforePrimedForSuspensionValidators,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })
const pagingVariables = {
	firstSuspendedValidators,
	lastSuspendedValidators,
	afterSuspendedValidators,
	beforeSuspendedValidators,
	firstPrimedForSuspensionValidators,
	lastPrimedForSuspensionValidators,
	afterPrimedForSuspensionValidators,
	beforePrimedForSuspensionValidators,
}
const { data, error, componentState } =
	useSuspendedValidatorsQuery(pagingVariables)
</script>
