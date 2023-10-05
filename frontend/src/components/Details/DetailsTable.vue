<template>
	<div>
		<Table :class="[$style.table, $style.contractDetail]">
			<slot />
		</Table>
		<div :class="$style['grid-container']">
			<div>&nbsp;</div>
			<div stydle="display: flex; justify-content: center;">
				<PaginationOffset 
					:total-count="props.totalCount"
					:info="pageOffsetInfo"
				/>
			</div>
			<div style="display: flex; justify-content: flex-end;">
				<PageDropdown :page-dropdown-info="props.pageDropdownInfo"/>
			</div>
		</div>
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
div.grid-container {
	display: grid;
	grid-template-columns: repeat(3, auto);
}
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
