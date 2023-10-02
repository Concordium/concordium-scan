<template>
	<td class="py-3 px-3 whitespace-nowrap align-top" :class="variant">
		<slot />
	</td>
</template>

<script lang="ts" setup>
type Props = {
	variant?: 'normal' | 'with-background'
}

defineProps<Props>()
</script>

<style scoped>
.with-background {
	background-color: var(--color-thead-bg);
}

/*
	1. Using first:/after: classes on the element seems not to work as desired
	2. Using @apply will prevent tests from running
*/
.with-background:first-child {
	border-radius: 8px 0 0 8px;
}

.with-background:last-child {
	border-radius: 0 8px 8px 0;
}

.with-background:only-child {
	border-radius: 8px;
}
</style>
