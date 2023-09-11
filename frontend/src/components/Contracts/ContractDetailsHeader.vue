<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<ChipIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Contract</h3>
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl numerical">
					{{ contract.contractAddress }}
				</h1>
				<TextCopy
					:text="contract.contractAddress"
					label="Click to copy contract address to clipboard"
					class="mx-3"
					icon-size="h-5 w-5"
					tooltip-class="font-sans"
				/>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import { ChipIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import type { Contract } from '~/types/generated'
type Props = {
	contract: Contract
}
defineProps<Props>()
</script>

<style module>
.title {
	max-width: calc(100% - 40px);
}
</style>
