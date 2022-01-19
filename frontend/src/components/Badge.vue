<template>
	<aside
		class="inline font-mono text-sm rounded-full align-middle uppercase ml-4 px-4 py-2 pt-3"
		:class="cssClass"
	>
		<slot />
	</aside>
</template>

<script lang="ts" setup>
type Props = {
	type: 'success' | 'failure'
}

const props = defineProps<Props>()

const cssClass = ref(`badge--${props.type}`)

watch(
	() => props.type,
	value => {
		cssClass.value = `badge--${value}`
	}
)
</script>

<style>
.badge--success {
	background-color: hsl(var(--color-interactive));
	color: hsl(var(--color-interactive-dark));
}

.badge--failure {
	background-color: hsl(var(--color-error));
	color: hsl(var(--color-error-dark));
}
</style>
