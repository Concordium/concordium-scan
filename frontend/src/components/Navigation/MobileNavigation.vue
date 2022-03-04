<template>
	<div class="md:hidden">
		<button
			type="button"
			aria-label="Open navigation"
			@click="() => (isNavigationOpen = true)"
		>
			<MenuIcon class="h-6" />
		</button>
		<Drawer
			:is-mobile="true"
			:is-open="isNavigationOpen"
			:on-close="() => (isNavigationOpen = false)"
		>
			<template #content>
				<nav class="flex flex-col items-end p-6 pt-24">
					<div
						v-for="route in navRoutes"
						:key="route.path"
						class="py-3"
						@click="isNavigationOpen = false"
					>
						<NuxtLink :to="route.path" class="text-2xl font-bold">
							{{ route.title }}
						</NuxtLink>
					</div>
				</nav>
			</template>
		</Drawer>
	</div>
</template>

<script lang="ts" setup>
import { MenuIcon } from '@heroicons/vue/outline/index.js'
import Drawer from '~/components/Drawer/Drawer.vue'
import type { Route } from '~/types/route'

const isNavigationOpen = ref(false)

type Props = {
	navRoutes: Route[]
}

defineProps<Props>()
</script>

<style>
.router-link-active {
	color: hsl(var(--color-interactive));
}
</style>
