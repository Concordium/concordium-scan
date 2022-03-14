<template>
	<div v-if="blockQueryResult.data">
		<BlockDetailsContent
			v-if="$route.params.internalId && blockQueryResult.data.block"
			:block="blockQueryResult.data.block"
			:go-to-page="goToPage"
		/>
		<BlockDetailsContent
			v-else
			:block="blockQueryResult.data.blockByBlockHash"
			:go-to-page="goToPage"
		/>
	</div>
</template>
<script lang="ts" setup>
import { Ref } from 'vue'
import BlockDetailsContent from '~/components/BlockDetails/BlockDetailsContent.vue'
import { useBlockQueryByHash, useBlockQuery } from '~/queries/useBlockQuery'
import { usePagination } from '~/composables/usePagination'

const { first, last, after, before, goToPage } = usePagination()

const route = useRoute()
const blockQueryResult = ref()

const blockHashRef = ref(route.params.blockHash)
const internalIdRef = ref(route.params.internalId)

if (!route.params.internalId)
	blockQueryResult.value = useBlockQueryByHash(blockHashRef as Ref<string>, {
		first,
		last,
		after,
		before,
	})
else
	blockQueryResult.value = useBlockQuery(internalIdRef as Ref<string>, {
		first,
		last,
		after,
		before,
	})
</script>
