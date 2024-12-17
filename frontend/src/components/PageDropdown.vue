<template>
	<div :class="$style['drop-down']">
		<div style="padding-right: 2px">Show:</div>
		<select
			class="form-select"
			:value="choosen"
			:class="$style.select"
			@input="onChange"
		>
			<option
				v-for="dropDown in dropdowns"
				:key="dropDown"
				class="bg-theme-background-primary"
				:value="dropDown"
			>
				{{ dropDown }}
			</option>
		</select>
		<div style="padding-left: 5px">Records</div>
	</div>
</template>
<script lang="ts" setup>
import {
	DEFAULT_PAGE_SIZE,
	MIN_PAGE_SIZE,
	type PageDropdownInfo,
} from '~/composables/usePageDropdown'

type Props = {
	pageDropdownInfo: PageDropdownInfo
}

const props = defineProps<Props>()

const choosen = ref(props.pageDropdownInfo.take.value)

const defaultDropDownValues = [MIN_PAGE_SIZE, DEFAULT_PAGE_SIZE, 25, 50, 100]
// Merge in the value in props
const dropdowns = computed(() => [
	// remove duplicates values and sort by constructing a Set and then back to an array.
	...new Set(
		[...defaultDropDownValues, props.pageDropdownInfo.take.value].sort()
	),
])

const onChange = (event: Event) => {
	const target = event.target as HTMLSelectElement
	const newTake = parseInt(target.value)
	props.pageDropdownInfo.update(newTake)
	choosen.value = newTake
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
