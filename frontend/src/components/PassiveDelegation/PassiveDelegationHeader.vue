<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<DelegationIcon class="w-12 h-12 mr-4 hidden md:block" />
		<div class="flex flex-wrap flex-grow w-1/2">
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl">Passive Delegation</h1>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import DelegationIcon from '../icons/DelegationIcon.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
</script>
