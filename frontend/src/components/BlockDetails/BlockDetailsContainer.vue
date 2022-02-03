<template>
	<div v-if="data?.block">
		<BlockDetailsContent :block="data.block" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import { useBlockQuery } from '~/queries/useBlockQuery'
import { usePagination } from '~/composables/usePagination'

const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id: string
}
const props = defineProps<Props>()
const { data } = useBlockQuery(props.id, { first, last, after, before })
</script>
