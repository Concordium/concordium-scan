<template>
	<td :class="$style.td">
		<slot />
	</td>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
	name: 'TableTd',
	inheritAttrs: true,
	props: {},
})
</script>

<style module>
.td {
	@apply px-6 py-3;
}
</style>
