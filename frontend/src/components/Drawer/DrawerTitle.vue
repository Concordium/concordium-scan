<template>
	<header
		class="z-20 flex-grow-0 sticky top-0 p-4 md:p-8 pr-16"
		:class="$style.drawerTitle"
	>
		<DrawerNavigation />

		<slot />
	</header>
</template>
<script lang="ts" setup>
import DrawerNavigation from '~/components/Drawer/DrawerNavigation.vue'
</script>

<style module>
.drawerTitle {
	background: hsl(247, 40%, 18%);
}

.drawerTitle::after {
	content: '';
	position: absolute;
	top: 0;
	width: 100%;
	height: 100%;
	pointer-events: none;
}
</style>
