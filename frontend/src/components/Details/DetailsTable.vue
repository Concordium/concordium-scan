<template>
	<div>
		<PageDropdown
			v-if="MIN_PAGE_SIZE < totalCount"
			:page-dropdown-info="props.pageDropdownInfo"
		/>
		<Table
			:class="[
				'contract-detail',
				{
					'no-last': totalCount <= props.pageOffsetInfo.take.value,
					fetching: fetching,
				},
			]"
		>
			<slot />
		</Table>
		<PaginationOffset :total-count="props.totalCount" :info="pageOffsetInfo" />
	</div>
</template>

<script lang="ts" setup>
import PaginationOffset from '../PaginationOffset.vue'
import PageDropdown from '../PageDropdown.vue'
import {
	MIN_PAGE_SIZE,
	type PageDropdownInfo,
} from '~~/src/composables/usePageDropdown'
import type { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'

type Props = {
	totalCount: number
	pageOffsetInfo: PaginationOffsetInfo
	pageDropdownInfo: PageDropdownInfo
	fetching: boolean
}

const props = defineProps<Props>()
</script>
<style>
.contract-detail table td {
	padding: 30px 20px 21px;
	@media screen and (max-width: 640px) {
		padding: 10px 20px;
	}
}
.contract-detail table tr {
	border-bottom: 2px solid;
	border-bottom-color: var(--color-thead-bg);
}

.fetching table tr {
	opacity: 0.4;
}

.contract-detail table thead tr {
	border-bottom: none;
}

.no-last table tr:last-child {
	border-bottom: none;
}
</style>
