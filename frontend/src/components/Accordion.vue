<template>
	<aside class="mb-1">
		<header
			class="w-full flex justify-between items-center transition-colors rounded-lg p-4"
			:class="$style.accordion"
			@click="toggleOpenState"
		>
			<h3 class="text-xl align-middle"><slot /></h3>
			<ChevronRightIcon
				:class="['h-8 transition-transform', { 'icon-open': isOpen }]"
			/>
		</header>
		<article v-show="isOpen" class="py-6">
			<slot name="content" />
		</article>
	</aside>
</template>

<script lang="ts" setup>
import { ChevronRightIcon } from '@heroicons/vue/solid/index.js'

const isOpen = ref(false)

const toggleOpenState = () => {
	isOpen.value = !isOpen.value
}
</script>

<style module>
.accordion {
	background-color: hsla(245, 24%, 51%, 20%);
	cursor: pointer;
}

.accordion:hover {
	background-color: hsla(245, 24%, 51%, 25%);
}
</style>

<style>
.icon-open {
	transform: rotate(90deg);
}
</style>
