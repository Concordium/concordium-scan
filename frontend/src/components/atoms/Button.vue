<template>
	<button
		class="rounded text-white bg-theme-button-primary hover:bg-theme-button-primary-hover px-8 py-3"
		@click="onClick"
	>
		Hello
	</button>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue'

export default defineComponent({
	name: 'Button',
	inheritAttrs: true,
	props: {
		onClick: {
			type: Function as PropType<() => void>,
			default: () => {
				/* */
			},
		},
	},
})
</script>
