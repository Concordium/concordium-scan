<template>
	<div v-if="data?.block">
		<BlockDetailsContent :block="data.block" />
	</div>
</template>

<script lang="ts" setup>
import { useBlockQuery } from '~/queries/useBlockQuery'

type Props = {
	id: string
}
const props = defineProps<Props>()
const { data } = useBlockQuery(props.id)
</script>
