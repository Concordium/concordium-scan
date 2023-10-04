<template>
	<div>
		<Table :class="[$style.table, $style.contractDetail]">
			<slot />
		</Table>
		<PaginationOffset 
				:total-count="props.totalCount"
				:info="pageOffsetInfo"
		/>
	</div>
</template>

<script lang="ts" setup>
import { useDateNow } from '../../composables/useDateNow'
import PaginationOffset from '../PaginationOffset.vue'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'

const { NOW } = useDateNow()

type Props = {
	totalCount: number
	pageOffsetInfo: PaginationOffsetInfo
}
const props = defineProps<Props>()
</script>
<style module>
.contractDetail table td {
	padding: 30px 20px 21px;
}
.table tr {
	border-bottom: 2px solid;
	border-bottom-color: var(--color-thead-bg);
}
.table tr:last-child {
	border-bottom: none;
}
</style>
