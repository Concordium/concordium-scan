<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<BakerIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Baker</h3>
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl numerical">
					{{ baker.bakerId }}
				</h1>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import BakerIcon from '~/components/icons/BakerIcon.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import type { Baker } from '~/types/generated'

type Props = {
	baker: Baker
}

defineProps<Props>()
</script>
