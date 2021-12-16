<template>
	<button class="text-purple-500 hover:text-purple-400" @click="onClick">
		<slot />
	</button>
</template>

<script lang="ts">
import { defineComponent, PropType } from 'vue'

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
