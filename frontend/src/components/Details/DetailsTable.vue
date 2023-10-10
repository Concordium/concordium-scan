<template>
	<div>
		<PageDropdown :page-dropdown-info="props.pageDropdownInfo"/>
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
import PaginationOffset from '../PaginationOffset.vue'
import PageDropdown from '../PageDropdown.vue'
import { PageDropdownInfo } from '~~/src/composables/usePageDropdown'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'

type Props = {
	totalCount: number
	pageOffsetInfo: PaginationOffsetInfo
	pageDropdownInfo: PageDropdownInfo
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

.table thead tr {
	border-bottom: none;
}

</style>
