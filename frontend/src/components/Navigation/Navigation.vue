<template>
	<div class="flex justify-end">
		<MobileNavigation :nav-routes="routes" />

		<DesktopNavigation :nav-routes="routes" />
	</div>
</template>

<script lang="ts" setup>
import MobileNavigation from './MobileNavigation.vue'
import DesktopNavigation from './DesktopNavigation.vue'
import type { Route } from '~/types/route'

const {
	public: { enablePltFeatures },
} = useRuntimeConfig()

const allRoutes: Route[] = [
	{
		title: 'Home',
		path: '/',
	},
	{
		title: 'Blocks',
		path: '/blocks',
	},
	{
		title: 'Transactions',
		path: '/transactions',
	},
	{
		title: 'Accounts',
		path: '/accounts',
	},
	{
		title: 'Staking',
		path: '/staking',
	},
	{
		title: 'Nodes',
		path: '/nodes',
	},
	{
		title: 'Contracts',
		path: '/contracts',
	},
	{
		title: 'CIS-2 Tokens',
		path: '/tokens',
	},
	{
		title: 'Stable Coin',
		path: '/stable-coin',
	},
]

// Now, filter it based on env
const routes = enablePltFeatures
	? allRoutes
	: allRoutes.filter(route => route.title !== 'Stable Coin')
</script>
