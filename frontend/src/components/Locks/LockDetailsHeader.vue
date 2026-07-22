<template>
	<DrawerTitle class="flex flex-row flex-wrap">
		<LockClosedIcon class="w-10 h-10 mr-6 hidden md:block" />

		<div class="flex flex-wrap flex-grow w-1/2">
			<h3 class="w-full text-sm text-theme-faded">Lock</h3>
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl numerical">
					{{ shortenHash(lock.lockId) }}
				</h1>
				<TextCopy
					:text="lock.lockId"
					label="Click to copy lock ID to clipboard"
					class="mx-3"
					icon-size="h-5 w-5"
					tooltip-class="font-sans"
				/>
				<Badge :type="statusBadgeType">
					{{ lock.status.toLowerCase() }}
				</Badge>
			</div>
		</div>
	</DrawerTitle>
</template>

<script lang="ts" setup>
import { LockClosedIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import Badge from '~/components/Badge.vue'
import TextCopy from '~/components/atoms/TextCopy.vue'
import { shortenHash } from '~/utils/format'
import { LockStatus, type Lock } from '~/types/generated'

type Props = {
	lock: Lock
}

const props = defineProps<Props>()

const statusBadgeType = computed(() => {
	if (props.lock.status === LockStatus.Active) return 'success'
	if (props.lock.status === LockStatus.Expired) return 'info'
	return 'failure'
})
</script>
