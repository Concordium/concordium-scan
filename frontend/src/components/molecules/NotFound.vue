<template>
	<DrawerContent class="flex flex-col items-center pt-8">
		<InfoIcon class="h-12 w-12 mb-6 text-theme-info" />
		<h1 class="text-xl"><slot>Not found</slot></h1>
		<p class="text-theme-faded">
			<slot name="secondary">Please check the address and try again</slot>
		</p>
	</DrawerContent>
</template>

<script lang="ts" setup>
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import InfoIcon from '~/components/icons/InfoIcon.vue'
</script>
