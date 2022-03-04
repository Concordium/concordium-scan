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
	<BWCubeLogoIcon
		v-else
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import { useBlockQuery, useBlockQueryByHash } from '~/queries/useBlockQuery'
import { usePagination } from '~/composables/usePagination'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
const { first, last, after, before, goToPage } = usePagination()
type Props = {
	id?: string
	hash?: string
}
const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refHash = toRef(props, 'hash')
const blockQueryResult = ref()
if (props.id) {
	blockQueryResult.value = useBlockQuery(refId as Ref<string>, {
		first,
		last,
		after,
		before,
	})
} else if (props.hash) {
	blockQueryResult.value = useBlockQueryByHash(refHash as Ref<string>, {
		first,
		last,
		after,
		before,
	})
}
</script>
