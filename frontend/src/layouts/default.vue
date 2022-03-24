<template>
	<div>
		<Title>CCDScan</Title>
		<Link rel="icon" href="/favicon.svg" />

		<Breakpoint v-if="environment === 'dev'" />

		<DrawerContainer />
		<div id="app" class="bg-theme-background-primary w-screen min-h-screen">
			<ClientOnly>
				<GraphQLClient>
					<PageHeader />

					<main class="p-4 pb-0 xl:container xl:mx-auto">
						<slot />
					</main>
				</GraphQLClient>

				<template #fallback>
					<div class="flex h-screen justify-center items-center">
						<BWCubeLogoIcon class="w-10 h-10 animate-ping" />
					</div>
				</template>
			</ClientOnly>
		</div>
	</div>
</template>

<script setup lang="ts">
import PageHeader from '~/components/PageHeader.vue'
import Breakpoint from '~/components/molecules/Breakpoint.vue'
import GraphQLClient from '~/components/molecules/GraphQLClient.vue'
import DrawerContainer from '~/components/Drawer/DrawerContainer.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import { useDrawer } from '~/composables/useDrawer'

useMeta({
	meta: [{ link: [{ rel: 'icon', href: '/favicon.svg' }] }],
})

const { environment } = useRuntimeConfig()

const route = useRoute()

const {
	updateByRouteData: drawerupdateByRouteData,
	handleInitialLoad: drawerhandleInitialLoad,
} = useDrawer()

drawerhandleInitialLoad(route)
watch(route, to => {
	drawerupdateByRouteData(to)
})
</script>
