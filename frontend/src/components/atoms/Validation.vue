<template>
	<span
		class="relative"
	>
		<transition name="tooltip">
			<span
				v-show="isVisible"
				class="text-sm p-3 rounded-lg tooltip"
				:class="textClass"
			>
                {{ text }}
			</span>
		</transition>
		<slot />
	</span>
</template>
<script lang="ts" setup>
import { useTooltip } from '~/composables/useTooltip'

type Props = {
	text: string
    isVisible: boolean
    textClass?: string
}
defineProps<Props>();

const {
	tooltipX,
	tooltipY,
	tooltipTransformYTo
} = useTooltip()

</script>
<style>
.tooltip {
	color: hsl(var(--color-error));
	background: var(--color-tooltip-bg);
	transform: translate(-50%, calc(v-bind(tooltipTransformYTo)));
	top: v-bind(tooltipY);
	left: v-bind(tooltipX);
	position: absolute;
	white-space: normal;
	text-align: center;
}

</style>
