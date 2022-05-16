<template>
	<header
		class="xl:container xl:mx-auto max-w-screen flex items-center md:justify-between p-5 md:mb-20 gap-4"
	>
		<div class="flex lg:w-1/3 align-middle items-center">
			<NuxtLink to="/" class="font-bold">
				<LogoWithText class="w-32 hidden lg:block" />
				<LogoNoText class="h-6 lg:hidden block align-middle" />
			</NuxtLink>

			<NetworkSelect class="hidden lg:block" />
		</div>

		<Search />

		<Navigation />
	</header>
</template>

<script lang="ts" setup>
import Navigation from '~/components/Navigation/Navigation.vue'
import NetworkSelect from '~/components/molecules/NetworkSelect.vue'
import LogoWithText from '~/components/icons/LogoWithText.vue'
import LogoNoText from '~/components/icons/LogoNoText.vue'
import Search from '~/components/Search/Search.vue'
</script>
