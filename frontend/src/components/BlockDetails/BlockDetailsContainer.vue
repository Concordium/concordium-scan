﻿<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<BlockDetailsContent
		v-else-if="componentState === 'success' && data"
		:block="data"
		:go-to-page-tx="goToPageTx"
	/>
</template>

<script lang="ts" setup>
import { useBlockQuery } from '~/queries/useBlockQuery'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import BlockDetailsContent from '~/components/BlockDetails/BlockDetailsContent.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'

// transaction pagination variables
const {
	first: firstTx,
	last: lastTx,
	after: afterTx,
	before: beforeTx,
	goToPage: goToPageTx,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const paginationVars = {
	firstTx,
	lastTx,
	afterTx,
	beforeTx,
}

type Props = {
	hash: string
}
const props = defineProps<Props>()
const refHash = toRef(props, 'hash')

const { data, error, componentState } = useBlockQuery({
	hash: refHash,
	eventsVariables: paginationVars,
})
</script>
