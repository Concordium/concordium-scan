<template>
	<table :class="$style.table">
		<slot />
	</table>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
export default defineComponent({
	name: 'Table',
	inheritAttrs: true,
})
</script>

<style module>
.table {
	@apply w-full text-white;
}
</style>
