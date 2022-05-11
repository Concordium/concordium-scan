<template>
	<div
		class="px-2 rounded-full text-xs uppercase tracking-wider"
		:class="{
			'bg-theme-background-interactive': variant !== 'secondary',
			'text-theme-interactiveDark': variant !== 'secondary',
			'text-theme-interactive': variant === 'secondary',
			border: variant === 'secondary',
			'border-solid': variant === 'secondary',
			'border-current': variant === 'secondary',
		}"
	>
		<slot />
	</div>
</template>

<script lang="ts" setup>
type Props = {
	variant?: 'primary' | 'secondary'
}

defineProps<Props>()
</script>
