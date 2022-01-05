<template>
	<Drawer :is-open="props.isOpen" :on-close="props.onClose">
		<DrawerTitle>9dd9ca</DrawerTitle>
		<DrawerContent>
			<div class="grid gap-6 grid-cols-2">
				<DetailsCard>
					<template #title>Timestamp</template>
					<template #default>Less than a minute ago</template>
					<template #secondary>2020-02-14 13:37:42Z</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Baker id</template>
					<template #default
						><UserIcon class="h-5 inline align-baseline mr-3" />6</template
					>
				</DetailsCard>
			</div>
		</DrawerContent>
		<DrawerActions>
			<Button :on-click="props.onClose" class="self-end">Close</Button>
		</DrawerActions>
	</Drawer>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid'
import Drawer from '~/components/Drawer/Drawer.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DrawerActions from '~/components/Drawer/DrawerActions.vue'
import Button from '~/components/atoms/Button.vue'
import DetailsCard from '~/components/DetailsCard.vue'

type Props = {
	isOpen: boolean
	onClose: () => void
}

const props = defineProps<Props>()
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-green-600;
}
.cellIcon {
	@apply h-4 text-white inline align-baseline;
}

.numerical {
	@apply font-mono;
}
</style>
