<template>
	<header class="z-10" :class="$style.drawerTitle">
		<DrawerNavigation />

		<slot />
	</header>
</template>
<script lang="ts" setup>
import DrawerNavigation from '~/components/Drawer/DrawerNavigation.vue'
</script>

<style module>
.drawerTitle {
	@apply flex-grow-0 sticky top-0 p-8 pr-16 mb-20;
	background: hsl(247, 40%, 18%);
}

.drawerTitle::after {
	@apply absolute top-0 w-full h-full pointer-events-none;
	content: '';
	box-shadow: 0 0 50px 10px hsl(247, 40%, 18%);
}
</style>
