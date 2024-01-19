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

					<main
						class="p-4 xl:container xl:mx-auto"
						style="margin-bottom: 100px"
					>
						<slot />
					</main>
					<footer class="footer">
						<div class="footer-rights">
							CCDScan (C) 2023 All rights reserved
						</div>
						<div class="footer-powered">
							<div>Powered by Concordium Blockchain</div>
							<div>
								Creating a future based on trust, privacy and safety
								<br />
								with blockchain technology
							</div>
						</div>
						<div class="footer-about">
							<div class="footer-about-about">About</div>
							<div class="footer-about-feedback">Feedback</div>
							<div class="footer-about-privacy">Privacy Policy</div>
							<div class="footer-about-ccd">CCD</div>
							<div class="footer-about-contact">Contact Us</div>
							<div class="footer-about-developers">Developers</div>
						</div>
						<div class="footer-follow">Follow us</div>
					</footer>
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

<style>
.footer-about-about {
	grid-column: 1 / 2;
	grid-row: 1 / 4;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 1 / 2;
	}
}
.footer-about-feedback {
	grid-column: 2 / 3;
	grid-row: 1 / 4;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 2 / 3;
	}
}
.footer-about-privacy {
	grid-column: 3 / 4;
	grid-row: 1 / 4;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 3 / 4;
	}
}
.footer-about-ccd {
	grid-column: 1 / 2;
	grid-row: 4 / 7;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 4 / 5;
	}
}
.footer-about-contact {
	grid-column: 2 / 3;
	grid-row: 4 / 7;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 5 / 6;
	}
}
.footer-about-developers {
	grid-column: 3 / 4;
	grid-row: 4 / 7;

	@media screen and (max-width: 640px) {
		grid-column: 1 / 4;
		grid-row: 6 / 7;
	}
}

.footer-powered {
	grid-column: 1 / 3;
	grid-row: 1 / 7;

	@media screen and (max-width: 961px) {
		grid-column: 1 / 4;
		grid-row: 1 / 4;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 1 / 3;
	}
}
.footer-about {
	grid-column: 3 / 5;
	grid-row: 1 / 7;
	display: grid;
	grid-template-columns: repeat(3, auto);
	grid-template-rows: repeat(6, auto);

	@media screen and (max-width: 961px) {
		grid-column: 1 / 4;
		grid-row: 4 / 7;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 3 / 5;
	}
}
.footer-follow {
	grid-column: 5 / 7;
	grid-row: 1 / 7;

	@media screen and (max-width: 961px) {
		grid-column: 4 / 7;
		grid-row: 1 / 7;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 5 / 7;
	}
}
.footer-rights {
	grid-column: 1 / 7;
	grid-row: 7 / 8;

	/* @media screen and (max-width: 961px) {
		grid-column: 1 / 7;
		grid-row: 7 / 9;
	}

	@media screen and (max-width: 640px) {
		grid-column: 1 / 7;
		grid-row: 5 / 7;
	} */
}
.footer {
	display: grid;
	position: fixed;
	left: 0;
	bottom: 0;
	width: 100%;
	background-color: red;
	height: 100px;
	grid-template-columns: repeat(6, auto);
	grid-template-rows: repeat(7, auto);

	@media screen and (max-width: 961px) {
		height: 200px;
	}

	@media screen and (max-width: 640px) {
		height: 300px;
	}
}
.page-margin {
	margin-bottom: 100px;

	@media screen and (max-width: 961px) {
		margin-bottom: 200px;
	}

	@media screen and (max-width: 640px) {
		margin-bottom: 300px;
	}
}
</style>
