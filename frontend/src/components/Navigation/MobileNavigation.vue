<template>
	<div class="2xl:hidden">
		<button
			type="button"
			aria-label="Open navigation"
			@click="() => (isNavigationOpen = true)"
		>
			<MenuIcon class="h-6" />
		</button>
		<transition v-if="isNavigationOpen" name="drawer">
			<Drawer
				class="flex flex-col flex-nowrap justify-between min-h-screen w-full md:w-3/4 xl:w-1/2 absolute top-0 right-0 z-20 overflow-x-hidden"
				:class="$style.drawer"
				:is-mobile="true"
				:is-open="isNavigationOpen"
				:on-close="() => (isNavigationOpen = false)"
			>
				<template #content>
					<div class="absolute right-6 top-5 z-20 p-2 flex gap-4">
						<button
							class="rounded hover:bg-theme-button-primary-hover transition-colors"
							aria-label="Close"
							@click="() => (isNavigationOpen = false)"
						>
							<XIcon class="h-6" />
						</button>
					</div>
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
		</transition>
	</div>
</template>

<script lang="ts" setup>
import { MenuIcon } from '@heroicons/vue/outline/index.js'
import { XIcon } from '@heroicons/vue/solid/index.js'
import Drawer from '~/components/Drawer/Drawer.vue'
import type { Route } from '~/types/route'

const isNavigationOpen = ref(false)

type Props = {
	navRoutes: Route[]
}

defineProps<Props>()
</script>
<style module>
.drawer {
	background: hsl(247, 40%, 18%);
	box-shadow: -25px 0 50px -12px var(--color-shadow-dark);
}
</style>
<style>
.router-link-active {
	color: hsl(var(--color-interactive));
}
</style>
