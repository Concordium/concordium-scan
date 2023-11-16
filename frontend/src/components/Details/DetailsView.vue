<template>
	<div
		:id="ID"
		class="flex flex-wrap gap-x-16 gap-y-10 detail-container"
		:class="[{ collapsed: !isOpen }]"
	>
		<button
			v-if="addButton"
			class="click-btn detail-expand-btn"
			:aria-expanded="isOpen"
			:aria-controls="ID"
			@click="toggleOpenState"
		>
			<ChevronRightIcon
				:class="['h-8 transition-transform', { 'icon-open': isOpen }]"
				aria-hidden
			/>
		</button>
		<slot />
	</div>
</template>
<script lang="ts" setup>
import { onMounted, ref } from 'vue'
import { ChevronRightIcon } from '@heroicons/vue/solid/index.js'

type Props = {
	id: number
}
const props = defineProps<Props>()
const ID = `details-view-${props.id}-${crypto.randomUUID()}`

const addButton = ref(false)
const isOpen = ref(false)
const toggleOpenState = () => {
	isOpen.value = !isOpen.value
}

onMounted(() => {
	const scrollHeight = document.querySelector(`#${ID}`)?.scrollHeight
	addButton.value = scrollHeight !== undefined && (scrollHeight as number) > 52
})
</script>
<style>
.icon-open {
	transform: rotate(90deg);
	background-color: #787594;
}

.icon-open path {
	fill: var(--color-thead-bg);
}

.collapsed {
	max-height: 52px;
	overflow: hidden;
	transition: max-height 0.5s ease;
}

.detail-expand-btn {
	width: 40px;
	height: 40px;
	position: absolute;
	right: 0;
}

.detail-expand-btn[aria-expanded='true'] {
	background-color: #787594;
}

.detail-container {
	position: relative;
	padding-right: 50px;
}
</style>
