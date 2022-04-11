<template>
	<div v-if="blockQueryResult.data">
		<BlockDetailsContent
			:block="
				blockQueryResult.data.block || blockQueryResult.data.blockByBlockHash
			"
			:go-to-page-tx="goToPageTx"
			:go-to-page-finalization-rewards="goToPageFinalizationRewards"
		/>
	</div>
</template>
<script lang="ts" setup>
import type { Ref } from 'vue'
import BlockDetailsContent from '~/components/BlockDetails/BlockDetailsContent.vue'
import { useBlockQueryByHash, useBlockQuery } from '~/queries/useBlockQuery'
import { usePagination } from '~/composables/usePagination'

// transaction pagination variables
const {
	first: firstTx,
	last: lastTx,
	after: afterTx,
	before: beforeTx,
	goToPage: goToPageTx,
} = usePagination()

// finalization rewards pagination variables
const {
	first: firstFinalizationRewards,
	last: lastFinalizationRewards,
	after: afterFinalizationRewards,
	before: beforeFinalizationRewards,
	goToPage: goToPageFinalizationRewards,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

const paginationVars = {
	firstTx,
	lastTx,
	afterTx,
	beforeTx,
	firstFinalizationRewards,
	lastFinalizationRewards,
	afterFinalizationRewards,
	beforeFinalizationRewards,
}

const route = useRoute()
const blockQueryResult = ref()

const blockHashRef = ref(route.params.blockHash)
const internalIdRef = ref(route.params.internalId)

if (!route.params.internalId)
	blockQueryResult.value = useBlockQueryByHash(
		blockHashRef as Ref<string>,
		paginationVars
	)
else
	blockQueryResult.value = useBlockQuery(
		internalIdRef as Ref<string>,
		paginationVars
	)
</script>
