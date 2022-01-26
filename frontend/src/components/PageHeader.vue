<template>
	<header class="max-w-screen flex items-center md:justify-between p-5 mb-20">
		<NuxtLink to="/" class="font-bold">
			<logo-horizontal />
		</NuxtLink>

		<div class="flex-grow pointer-events-none md:hidden"></div>

		<search-input />

		<Navigation />
	</header>
</template>

<script lang="ts" setup>
import Navigation from '~/components/Navigation/Navigation.vue'
</script>
