<template>
	<div
		class="bar-wrapper rounded-full flex h-1 overflow-hidden"
		:style="`background-color: ${color}`"
	>
		<slot />
	</div>
</template>

<script lang="ts" setup>
type Props = {
	color?: string
}

defineProps<Props>()
</script>

<style scoped>
.bar-wrapper {
	background: hsla(var(--color-info-dark), 25%);
}
</style>
