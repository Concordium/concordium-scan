<template>
	<div>
		<PageDropdown 
			v-if="MIN_PAGE_SIZE < totalCount"
			:page-dropdown-info="props.pageDropdownInfo"/>
		<Table :class="['contractDetail', {'no-last': totalCount <= props.pageOffsetInfo.take.value}]">
			<slot />
		</Table>
		<PaginationOffset 
			:total-count="props.totalCount"
			:info="pageOffsetInfo"
		/>
	</div>
</template>

<script lang="ts" setup>
import PaginationOffset from '../PaginationOffset.vue'
import PageDropdown from '../PageDropdown.vue'
import { MIN_PAGE_SIZE, PageDropdownInfo } from '~~/src/composables/usePageDropdown'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'

type Props = {
	totalCount: number
	pageOffsetInfo: PaginationOffsetInfo
	pageDropdownInfo: PageDropdownInfo
}

const props = defineProps<Props>()

</script>
<style>
.contractDetail table td {
	padding: 30px 20px 21px;
}
.contractDetail table tr {
	border-bottom: 2px solid;
	border-bottom-color: var(--color-thead-bg);
}

.contractDetail table thead tr {
	border-bottom: none;
}

.no-last table tr:last-child {
	border-bottom: none;
}

</style>
