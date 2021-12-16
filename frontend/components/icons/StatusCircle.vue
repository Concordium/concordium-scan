<template>
	<svg
		xmlns="http://www.w3.org/2000/svg"
		viewBox="0 0 20 20"
		class="inline align-baseline"
	>
		<circle cx="10" cy="10" r="7" fill="currentColor" fill-opacity="1" />
		<circle cx="10" cy="10" r="10" fill="currentColor" fill-opacity="0.25" />
	</svg>
</template>

<script lang="ts">
import { defineComponent } from 'vue'

export default defineComponent({
	name: 'StatusIcon',
	inheritAttrs: true,
})
</script>
