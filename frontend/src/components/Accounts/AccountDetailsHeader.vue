<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<UserIcon class="w-12 h-12 mr-4 none md:block" />
		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Account</h3>
			<h1 class="inline-block text-2xl" :class="$style.title">
				<span class="numerical truncate inline-block w-full">
					{{ account.address }}
				</span>
			</h1>
			<TextCopy
				:text="account.address"
				label="Click to copy account address to clipboard"
				class="h-5 inline align-baseline mr-3"
				tooltip-class="font-sans"
			/>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import type { Account } from '~/types/generated'

type Props = {
	account: Account
}

defineProps<Props>()
</script>

<style module>
.title {
	max-width: calc(100% - 200px);
}
</style>
