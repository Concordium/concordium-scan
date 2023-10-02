<template>
	<nav class="hidden xl:flex -mr-4 lol flex-grow flex-shrink-0">
		<NuxtLink
			v-for="route in navRoutes"
			:key="route.path"
			:to="route.path"
			:class="$style.link"
		>
			<span :class="$style.linkInner">{{ route.title }}</span>
		</NuxtLink>
	</nav>
</template>

<script lang="ts" setup>
import type { Route } from '~/types/route'

type Props = {
	navRoutes: Route[]
}

defineProps<Props>()
</script>

<style module>
.link {
	@apply font-bold transition-colors py-2 relative;
}

.link:hover {
	color: hsl(var(--color-interactive));
}

.linkInner {
	@apply divide-x md:border-r-2 border-theme-white px-4;
}

.link:first-child .linkInner {
	@apply pl-0;
}

.link:last-child .linkInner {
	@apply border-r-0;
}
</style>

<style>
.router-link-active {
	color: hsl(var(--color-interactive));
}
</style>
