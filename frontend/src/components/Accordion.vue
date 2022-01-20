<template>
	<aside class="mb-1">
		<button
			class="w-full flex justify-between items-center transition-colors rounded-lg p-4"
			:class="$style.accordion"
			:aria-expanded="isOpen"
			:aria-controls="ID"
			@click="toggleOpenState"
		>
			<h3 class="text-xl align-middle"><slot /></h3>
			<ChevronRightIcon
				:class="['h-8 transition-transform', { 'icon-open': isOpen }]"
				aria-hidden
			/>
		</button>
		<article v-show="isOpen" :id="ID" class="p-4 py-6" :aria-hidden="!isOpen">
			<slot name="content" />
		</article>
	</aside>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { ChevronRightIcon } from '@heroicons/vue/solid/index.js'

const isOpen = ref(false)
const ID = `accordion-${Math.floor(Math.random() * 1000)}`

const toggleOpenState = () => {
	isOpen.value = !isOpen.value
}
</script>

<style module>
.accordion {
	background-color: var(--color-background-elevated);
	cursor: pointer;
}

.accordion:hover {
	background-color: var(--color-background-elevated-hover);
}
</style>

<style>
.icon-open {
	transform: rotate(90deg);
}
</style>
