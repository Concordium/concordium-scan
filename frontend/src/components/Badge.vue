<template>
	<aside
		class="inline font-mono text-sm rounded-full align-middle uppercase ml-4 px-4 py-2 pt-3"
		:class="$style.badge"
	>
		<slot />
	</aside>
</template>

<style module>
.badge {
	background-color: hsl(var(--color-interactive));
	color: hsl(var(--color-interactive-dark));
}
</style>
