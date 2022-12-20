<template>
	<header
		class="xl:container xl:mx-auto max-w-screen flex items-center md:justify-between p-5 md:mb-20 gap-4"
	>
		<div class="flex align-middle items-center">
			<NuxtLink to="/" class="font-bold">
				<LogoWithText class="w-32 hidden lg:block" />
				<LogoNoText class="h-6 lg:hidden block align-middle" />
				<Versions class="hidden lg:block" />
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
import Versions from '~/components/molecules/Versions.vue'
</script>
