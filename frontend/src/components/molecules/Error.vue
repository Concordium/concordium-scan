<template>
	<DrawerContent class="flex flex-col items-center pt-8">
		<WarningIcon class="h-12 w-12 mb-6 text-theme-error" />
		<h1 class="text-xl">Something went wrong</h1>
		<h3 class="text-theme-faded">We are very sorry. Please try again later.</h3>
		<p v-if="environment === 'dev'" class="mt-10">{{ error }}</p>
	</DrawerContent>
</template>

<script lang="ts" setup>
import type { CombinedError } from '@urql/vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import WarningIcon from '~/components/icons/WarningIcon.vue'

const {
	public: { environment },
} = useRuntimeConfig()

type Props = {
	error?: CombinedError
}
defineProps<Props>()
</script>
