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
	<BWCubeLogoIcon
		v-else
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import { useBlockQuery, useBlockQueryByHash } from '~/queries/useBlockQuery'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import BlockDetailsContent from '~/components/BlockDetails/BlockDetailsContent.vue'

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

type Props = {
	id?: string
	hash?: string
}
const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refHash = toRef(props, 'hash')
const blockQueryResult = ref()
if (props.id) {
	blockQueryResult.value = useBlockQuery(refId as Ref<string>, paginationVars)
} else if (props.hash) {
	blockQueryResult.value = useBlockQueryByHash(
		refHash as Ref<string>,
		paginationVars
	)
}
</script>
