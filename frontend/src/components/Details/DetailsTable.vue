<template>
	<div>
		<Table :class="[$style.table, $style.contractDetail]">
			<slot />
		</Table>
		<Pagination v-if="pageInfo" :page-info="pageInfo" :go-to-page="goToPage" />
	</div>
</template>

<script lang="ts" setup>
import { PageInfo } from '../../types/generated'
import { PaginationTarget } from '../../composables/usePagination'
import { useDateNow } from '../../composables/useDateNow'

const { NOW } = useDateNow()

type Props = {
	pageInfo: PageInfo
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
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
