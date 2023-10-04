<template>
	<div>
		<div
			:class="$style['drop-down']">
			<div style="padding-right: 2px;">Rows: </div>
			<select
				class="form-select"
				:value="choosen"
				:class="$style.select"
				@input="onChange"
			>
			<option
				v-for="dropDown in dropdowns" :key="dropDown"
				class="bg-theme-background-primary"
				:value="dropDown"
			>
				{{ dropDown }}
			</option>
			</select>
		</div>
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
import { PageDropdownInfo } from '~~/src/composables/usePageDropdown'
import { PaginationOffsetInfo } from '~~/src/composables/usePaginationOffset'

type Props = {
	totalCount: number
	pageOffsetInfo: PaginationOffsetInfo
	pageDropdownInfo: PageDropdownInfo
}

const props = defineProps<Props>()
const choosen = ref(props.pageDropdownInfo.take.value)

let dropDownValues = [5, 10, 25, 50, 100];
const dropdowns = computed(() => {
	if (!dropDownValues.includes(props.pageDropdownInfo.take.value)) {
	  dropDownValues.push(props.pageDropdownInfo.take.value);
	  dropDownValues = dropDownValues.sort(function (a, b) {  return a - b;  });
	}
	return dropDownValues;
})


const onChange = (event: Event) => {
	const target = event.target as HTMLSelectElement;
	const newTake = parseInt(target.value);
	props.pageDropdownInfo.update(newTake);
	choosen.value = newTake;
}

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

.drop-down {
	display: flex;
	justify-content: flex-end;
	padding-bottom: 10px;
}

.select {
	padding-left: 10px;
	margin-left: 10px;
	border-radius: 0.75rem;
	background-color: var(--color-thead-bg);
}
</style>
