<template>
	<article class="flex-grow px-2 sm:px-8" :class="$style.drawerContent">
		<slot />
	</article>
</template>

<style module>
.drawerContent {
	background: hsl(247, 40%, 18%);
}
</style>
