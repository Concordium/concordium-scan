<template>
	<span
		class="relative"
		@mouseenter="handleOnMouseEnter"
		@mouseleave="handleOnMouseLeave"
	>
		<transition name="tooltip">
			<span
				v-show="isVisible"
				class="text-sm left-1/2 top-0 w-max p-3 rounded-lg z-50 tooltip pointer-events-none"
				:class="textClass"
			>
				{{ text }}
				<slot name="content" />
				<span class="tooltip-triangle"></span>
			</span>
		</transition>
		<slot />
	</span>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { useTooltip } from '~/composables/useTooltip'
import type { Position } from '~/composables/useTooltip'

const isVisible = ref(false)

type Props = {
	text?: string
	textClass?: string
	position?: Position
	x?: string
	y?: string
	tooltipPosition?: string
	onMouseEnter?: () => void
	onMouseLeave?: () => void
}
const props = defineProps<Props>()
const tooltipPosition = ref(props.tooltipPosition || 'fixed')

const {
	triangleTopBorder,
	triangleBottomBorder,
	trianglePosTop,
	tooltipX,
	tooltipY,
	tooltipTransformYFrom,
	tooltipTransformYTo,
	calculateCoordinates,
} = useTooltip(props.position, props.x, props.y)

const handleOnMouseEnter = (event: MouseEvent) => {
	calculateCoordinates(event)
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
	transform: translate(-50%, calc(v-bind(tooltipTransformYTo)));
	top: v-bind(tooltipY);
	left: v-bind(tooltipX);
	position: v-bind(tooltipPosition);
}

/* Binding variables from a composable to a pseudo element does not work in Vue */
.tooltip-triangle {
	display: block;
	height: 10px;
	width: 10px;
	border-width: 0 10px 0;
	border-color: transparent;
	border-top: v-bind(triangleTopBorder) solid;
	border-bottom: v-bind(triangleBottomBorder) solid;
	border-bottom-color: var(--color-tooltip-bg);
	border-top-color: var(--color-tooltip-bg);
	position: absolute;
	top: v-bind(trianglePosTop);
	left: 50%;
	transform: translate(-10px, 0);
}

.tooltip-enter-active,
.tooltip-leave-active {
	transition: transform 0.2s ease-out, opacity 0.1s ease-in;
}

.tooltip-leave-active {
	transition: transform 0.1s ease-in, opacity 0.1s ease-in;
}

.tooltip-enter-from,
.tooltip-leave-to {
	transform: translate(-50%, calc(v-bind(tooltipTransformYFrom)));
	opacity: 0;
}

.tooltip-enter-to,
.tooltip-leave-from {
	transform: translate(-50%, calc(v-bind(tooltipTransformYTo)));
	opacity: 1;
}
</style>
