<template>
	<span
		class="relative"
		@mouseenter="handleOnMouseEnter"
		@mouseleave="handleOnMouseLeave"
	>
		<transition name="tooltip">
			<span
				v-show="isVisible"
				class="text-sm absolute -bottom-12 left-1/2 w-max p-3 rounded-lg z-50 tooltip"
			>
				{{ text }}
			</span>
		</transition>
		<slot />
	</span>
</template>

<script lang="ts" setup>
import { ref } from 'vue'

const isVisible = ref(false)

type Props = {
	text: string
	onMouseEnter?: () => void
	onMouseLeave?: () => void
}

const props = defineProps<Props>()

const handleOnMouseEnter = () => {
	isVisible.value = true
	props.onMouseEnter?.()
}

const handleOnMouseLeave = () => {
	isVisible.value = false
}
</script>

<style>
.tooltip {
	background: var(--color-tooltip-bg);
	box-shadow: 0 3px 6px 0 var(--color-shadow-dark);
	transform: translate(-50%, 0);
}

.tooltip::after {
	content: '';
	display: block;
	height: 10px;
	width: 10px;
	border-width: 0 0 10px;
	border-left: 10px solid transparent;
	border-right: 10px solid transparent;
	border-bottom: 10px solid var(--color-tooltip-bg);
	position: absolute;
	top: -10px;
	left: 50%;
	transform: translateX(-50%);
}

.tooltip-enter-active,
.tooltip-leave-active {
	transition: all 0.2s ease-out;
}

.tooltip-leave-active {
	transition: all 0.1s ease-in;
}

.tooltip-enter-from,
.tooltip-leave-to {
	transform: translate(-50%, -5px);
	opacity: 0;
}

.tooltip-enter-to,
.tooltip-leave-from {
	transform: translate(-50%, 0);
	opacity: 1;
}
</style>
