<template>
	<div v-if="blockQueryResult.data">
		<BlockDetailsContent
			v-if="$route.params.internalId && blockQueryResult.data.block"
			:block="blockQueryResult.data.block"
		/>
		<BlockDetailsContent
			v-else
			:block="blockQueryResult.data.blockByBlockHash"
		/>
	</div>
</template>
<script lang="ts" setup>
import BlockDetailsContent from '~/components/BlockDetails/BlockDetailsContent.vue'
import { useBlockQueryByHash, useBlockQuery } from '~/queries/useBlockQuery'
const route = useRoute()
const blockQueryResult = ref()
if (!route.params.internalId)
	blockQueryResult.value = useBlockQueryByHash(route.params.blockHash + '')
else blockQueryResult.value = useBlockQuery(route.params.internalId + '')
</script>
