<template>
	<Drawer :is-open="props.isOpen" :on-close="props.onClose">
		<DrawerTitle>9dd9ca</DrawerTitle>
		<DrawerContent
			>Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores nemo
			omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!Lorem ipsum dolor, sit amet consectetur adipisicing elit. Maiores
			nemo omnis dolorem, possimus laboriosam eos hic sunt ullam accusamus cum
			expedita rerum ducimus culpa odit iure ipsam architecto fugit
			incidunt!</DrawerContent
		>
		<DrawerActions>
			<Button :on-click="props.onClose" class="self-end">Close</Button>
		</DrawerActions>
	</Drawer>
</template>

<script lang="ts" setup>
import Drawer from '~/components/Drawer/Drawer.vue'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DrawerActions from '~/components/Drawer/DrawerActions.vue'
import Button from '~/components/atoms/Button.vue'

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
