<template>
	<th :class="$style.th">
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
	@apply px-6 py-3 font-normal text-left;
	background-color: var(--color-thead-bg);
}

.th:first-child {
	@apply rounded-l-lg;
}

.th:last-child {
	@apply rounded-r-lg;
}
</style>
