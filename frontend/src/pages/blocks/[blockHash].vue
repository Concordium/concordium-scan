<template>
	<div v-if="$route.params.internalId">
		<div v-if="blockQueryResult.data">
			<BlockDetailsContent :block="blockQueryResult.data.block" />
		</div>
	</div>
	<div v-else>
		<div v-if="blockQueryResult.data">
			<BlockDetailsContent :block="blockQueryResult.data.blockByBlockHash" />
		</div>
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
