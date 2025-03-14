<template>
	<span
		class="relative"
		@mouseenter="handleOnMouseEnter"
		@mouseleave="handleOnMouseLeave"
	>
		<span
			v-show="isVisible"
			class="text-sm left-1/2 p-3 rounded-lg z-50 tooltip pointer-events-none"
			:class="textClass"
		>
			{{ text }}
			<slot name="content" />
			<span class="tooltip-triangle" />
		</span>
		<slot />
	</span>
</template>

<script lang="ts" setup>
import { ref } from 'vue'
import { type Position, useTooltip } from '~/composables/useTooltip'

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
	pointer-events: auto;
	white-space: normal;
	text-align: center;
	overflow-wrap: break-word;
	max-inline-size: 200px;
	@media screen and (max-width: 640px) {
		max-inline-size: 150px;
	}
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
</style>
