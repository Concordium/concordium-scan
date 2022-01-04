<template>
	<article :class="$style.drawerContent">
		<slot />
	</article>
</template>

<style module>
.drawerContent {
	@apply flex-grow px-8;
	background: hsl(247, 40%, 18%);
}
</style>
