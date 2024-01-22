<template>
	<button class="hover:text-theme-interactiveHover" @click="handleClick">
		<slot />
	</button>
</template>
<script lang="ts" setup>
type Props = {
	url: string
}
const props = defineProps<Props>()
const handleClick = () => {
	window.open(props.url, '_blank')
}
</script>
<style>
.link-button:hover {
	color: red;
}
</style>
