<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<CurrencyDollarIcon class="w-12 h-12 mr-4 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">CIS2 Token</h3>
			<div class="w-full flex items-center justify-items-stretch">
				<h1
					class="text-2xl numerical"
					style="white-space: nowrap; overflow: hidden; text-overflow: ellipsis"
				>
					{{ tokenId }}
				</h1>

				<TextCopy
					:text="token.tokenId"
					label="Click to copy account address to clipboard"
					class="mx-3"
					icon-size="h-5 w-5"
					tooltip-class="font-sans"
				/>
			</div>
			<h2 class="text-theme-faded numerical">
				Index:{{ token.contractIndex }}, Subindex:{{ token.contractSubIndex }}
			</h2>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import { CurrencyDollarIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import type { Token } from '~/types/generated'

type Props = {
	token: Token
}

const props = defineProps<Props>()
const tokenId = computed(() => {
	const id = props.token.tokenId.trim()
	if (!id || id === '') {
		return '--'
	} else {
		return id
	}
})
</script>

<style module>
.title {
	max-width: calc(100% - 40px);
}
</style>
