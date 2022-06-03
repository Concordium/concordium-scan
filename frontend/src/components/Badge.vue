<template>
	<aside
		class="inline font-mono text-sm rounded-full align-middle uppercase ml-4 px-4 py-2 pt-3"
		:class="cssClass"
	>
		<slot />
	</aside>
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue'

type Props = {
	type: 'success' | 'failure' | 'info'
	variant?: 'primary' | 'secondary'
}

const props = defineProps<Props>()

const cssClass = ref(`badge--${props.type}-${props.variant || 'primary'}`)

watch(
	() => props.type,
	value => {
		cssClass.value = `badge--${value}`
	}
)
</script>

<style>
.badge--success-primary {
	background-color: hsl(var(--color-interactive));
	color: hsl(var(--color-interactive-dark));
}

.badge--success-secondary {
	border: solid 1px currentColor;
	color: hsl(var(--color-interactive));
}

.badge--failure-primary {
	background-color: hsl(var(--color-error));
	color: hsl(var(--color-error-dark));
}

.badge--failure-secondary {
	border: solid 1px currentColor;
	color: hsl(var(--color-error));
}

.badge--info-primary {
	background-color: hsl(var(--color-info));
	color: hsl(var(--color-info-dark));
}

.badge--info-secondary {
	border: solid 1px currentColor;
	color: hsl(var(--color-info));
}
</style>
