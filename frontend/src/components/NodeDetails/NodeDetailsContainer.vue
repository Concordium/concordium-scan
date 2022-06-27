<template>
	<Loader v-if="componentState === 'loading'" />
	<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
	<Error v-else-if="componentState === 'error'" :error="error" class="pt-20" />

	<NodeDetailsContent
		v-else-if="componentState === 'success' && data"
		:node="data.nodeStatus"
	/>
</template>

<script lang="ts" setup>
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { useNodeDetailQuery } from '~/queries/useNodeDetailQuery'
import NodeDetailsContent from '~/components/NodeDetails/NodeDetailsContent.vue'
type Props = {
	nodeInternalId: string
}

const props = defineProps<Props>()
const { data, error, componentState } = useNodeDetailQuery(props.nodeInternalId)
</script>
