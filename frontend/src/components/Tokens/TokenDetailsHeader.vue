<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<TokenIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">CIS2 Token</h3>
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl numerical">
					{{ tokenId || '-' }}
				</h1>
				<TextCopy
					:text="tokenId"
					label="Click to copy token id to clipboard"
					class="mx-3"
					icon-size="h-5 w-5"
					tooltip-class="font-sans"
				/>
			</div>
		</div>
	</DrawerTitle>
</template>
<script lang="ts" setup>
import DrawerTitle from '../Drawer/DrawerTitle.vue'
import TextCopy from '../atoms/TextCopy.vue'
import TokenIcon from '../icons/TokenIcon.vue'

type Props = {
	tokenId: string
}
defineProps<Props>()
</script>
