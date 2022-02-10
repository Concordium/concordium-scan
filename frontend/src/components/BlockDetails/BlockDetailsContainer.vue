<template>
	<div v-if="blockQueryResult.data">
		<BlockDetailsContent
			v-if="blockQueryResult.data.block"
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
import { useBlockQuery, useBlockQueryByHash } from '~/queries/useBlockQuery'
import { usePagination } from '~/composables/usePagination'
const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id?: string
	hash?: string
}
const props = defineProps<Props>()
const blockQueryResult = ref()
if (props.id)
	blockQueryResult.value = useBlockQuery(props.id, {
		first,
		last,
		after,
		before,
	})
else if (props.hash)
	blockQueryResult.value = useBlockQueryByHash(props.hash, {
		first,
		last,
		after,
		before,
	})
</script>
