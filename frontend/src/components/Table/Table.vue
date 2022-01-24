<template>
	<div class="max-w-full overflow-x-auto">
		<table class="w-full">
			<slot />
		</table>
	</div>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
export default defineComponent({
	name: 'Table',
	inheritAttrs: true,
})
</script>
