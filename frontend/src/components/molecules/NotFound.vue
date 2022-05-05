<template>
	<DrawerContent class="flex flex-col items-center pt-20">
		<InfoIcon class="h-12 w-12 mb-6 text-theme-info" />
		<h1 class="text-xl">Not found</h1>
		<h3 class="text-theme-faded">Please check the address and try again.</h3>
	</DrawerContent>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import InfoIcon from '~/components/icons/InfoIcon.vue'
</script>
