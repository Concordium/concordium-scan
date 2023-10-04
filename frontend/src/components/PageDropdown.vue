<template>
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
</template>
<script lang="ts" setup>
import { PageDropdownInfo } from '../composables/usePageDropdown'

type Props = {
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
