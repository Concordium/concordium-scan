<template>
	<div class="bar" :style="`width: ${width}%; background-color: ${color}`" />
</template>

<script lang="ts" setup>
type Props = {
	width: number
	color?: string
}

defineProps<Props>()
</script>

<style scoped>
.bar {
	background-color: hsl(var(--color-info));
}
</style>
