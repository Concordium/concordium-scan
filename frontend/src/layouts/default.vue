<template>
	<div class="bg-theme-background-primary w-screen min-h-screen">
		<Title>CCDScan</Title>
		<Link rel="icon" href="/favicon.svg" />

		<Breakpoint v-if="environment === 'dev'" />

		<ClientOnly>
			<GraphQLClient>
				<DrawerContainer />
				<div id="app">
					<PageHeader :class="[isLoading ? 'pointer-events-none' : ' ']" />
					<main class="p-4 xl:container xl:mx-auto">
						<slot />
					</main>
					<PageFooter />
				</div>
			</GraphQLClient>

			<template #fallback>
				<div class="flex h-screen w-screen justify-center items-center">
					<BWCubeLogoIcon class="w-10 h-10 animate-ping" />
				</div>
			</template>
		</ClientOnly>
	</div>
</template>
<script setup lang="ts">
import PageFooter from '../components/PageFooter/PageFooter.vue'
import PageHeader from '~/components/PageHeader.vue'
import Breakpoint from '~/components/molecules/Breakpoint.vue'
import GraphQLClient from '~/components/molecules/GraphQLClient.vue'
import DrawerContainer from '~/components/Drawer/DrawerContainer.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import { useDrawer } from '~/composables/useDrawer'

useHead({ meta: [{ link: [{ rel: 'icon', href: '/favicon.svg' }] }] })

const {
	public: { environment },
} = useRuntimeConfig()

const route = useRoute()
const {
	updateByRouteData: drawerupdateByRouteData,
	handleInitialLoad: drawerhandleInitialLoad,
} = useDrawer()

const isLoading = ref(false)
drawerhandleInitialLoad(route)
watch(route, to => {
	isLoading.value = true
	drawerupdateByRouteData(to)
	setTimeout(() => {
		// Forcing initial load of the route to be completed,
		// before the user can navigate to the next route.
		isLoading.value = false
	}, 1)
})
</script>
