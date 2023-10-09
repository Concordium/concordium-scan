<template>
	<th
		class="px-6 py-3 font-normal whitespace-nowrap"
		:class="$style.th"
		align="left"
	>
		<slot />
	</th>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
	name: 'TableTh',
	inheritAttrs: true,
})
</script>

<style module>
.th {
	background-color: var(--color-thead-bg);
}

/*
	1. Using first:/after: classes on the element seems not to work as desired
	2. Using @apply will prevent tests from running
*/
.th:first-child {
	border-radius: 8px 0 0 8px;
}

.th:last-child {
	border-radius: 0 8px 8px 0;
}
</style>
