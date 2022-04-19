<template>
	<header
		class="z-20 flex-grow-0 sticky top-0 p-8 pr-16 mb-20"
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
	box-shadow: 0 0 50px 10px hsl(247, 40%, 18%);
	position: absolute;
	top: 0;
	width: 100%;
	height: 100%;
	pointer-events: none;
}
</style>
