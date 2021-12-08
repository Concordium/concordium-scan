<template>
	<main>
		<p class="bg-purple-700 text-white p-6">Hello</p>
		<Button> Click! </Button>
	</main>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import Button from '~/components/atoms/Button.vue'

export default defineComponent({
	components: {
		Button,
	},
})
</script>
