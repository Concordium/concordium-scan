<template>
	<span class="relative">
		<transition name="validation-tooltip">
			<span
				v-show="isVisible"
				class="text-sm p-3 rounded-lg validation-tooltip"
				:class="textClass"
			>
				{{ text }}
			</span>
		</transition>
		<slot />
	</span>
</template>
<script lang="ts" setup>
type Props = {
	text: string
	isVisible: boolean
	textClass?: string
}
defineProps<Props>()
</script>
<style>
.validation-tooltip {
	color: hsl(var(--color-error));
	background: var(--color-tooltip-bg);
	top: -80px;
	left: -150px;
	position: absolute;
	white-space: normal;
	text-align: center;
	max-width: 200px;
}

.validation-tooltip-enter-active,
.validation-tooltip-leave-active {
	transition: opacity 0.5s ease;
}

.validation-tooltip-enter-from,
.validation-tooltip-leave-to {
	opacity: 0;
}
</style>
