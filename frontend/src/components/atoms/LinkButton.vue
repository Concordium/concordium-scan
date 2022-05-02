<template>
	<button
		class="text-theme-interactive hover:text-theme-interactiveHover"
		@click="onClick"
	>
		<slot />
	</button>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import type { PropType } from 'vue'

export default defineComponent({
	name: 'LinkButton',
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
