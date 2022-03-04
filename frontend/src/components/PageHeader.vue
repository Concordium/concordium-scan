<template>
	<header
		class="max-w-screen flex items-center md:justify-between p-5 md:mb-20 gap-4"
	>
		<NuxtLink to="/" class="font-bold">
			<LogoWithText class="w-32 hidden md:block" />
			<LogoNoText class="h-6 md:hidden block align-middle" />
		</NuxtLink>

		<div class="flex-grow pointer-events-none md:hidden"></div>

		<search-input />

		<Navigation />
	</header>
</template>

<script lang="ts" setup>
import Navigation from '~/components/Navigation/Navigation.vue'
import LogoWithText from '~/components/icons/LogoWithText.vue'
import LogoNoText from '~/components/icons/LogoNoText.vue'
</script>
