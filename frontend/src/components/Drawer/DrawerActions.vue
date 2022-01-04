<template>
	<footer :class="$style.drawerActions">
		<slot />
	</footer>
</template>

<style module>
.drawerActions {
	@apply flex flex-grow-0 sticky bottom-0 mt-8 p-8;
	background: hsl(247, 40%, 18%);
}

.drawerActions::after {
	@apply absolute top-0 w-full h-full pointer-events-none;
	content: '';
	box-shadow: 0 -20px 50px 10px hsl(247, 40%, 18%);
}
</style>
