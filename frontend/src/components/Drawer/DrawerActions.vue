<template>
	<footer :class="$style.drawerActions">
		<slot />
	</footer>
</template>

<style module>
.drawerActions {
	@apply bottom-0 mt-8 p-8;
	background: hsl(247, 40%, 18%);
}

.drawerActions::after {
	@apply absolute top-0 w-full h-full left-0 pointer-events-none;
	content: '';
}
</style>
