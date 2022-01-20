<template>
	<div>
		<transition name="drawer-mask">
			<div
				v-if="isOpen"
				:class="$style.drawerMask"
				@click="props.onClose"
			></div>
		</transition>

		<transition name="drawer">
			<section v-if="isOpen" :class="$style.drawer">
				<button :class="$style.closeButton" @click="props.onClose">
					<XIcon :class="$style.closeIcon" />
				</button>

				<div>
					<slot name="content" />
				</div>
				<slot name="actions" />
			</section>
		</transition>
	</div>
</template>

<script lang="ts" setup>
import { XIcon } from '@heroicons/vue/solid/index.js'

type Props = {
	isOpen: boolean
	onClose: () => void
}

const props = defineProps<Props>()

watch(
	() => props.isOpen,
	value => {
		const appEl = document.getElementById('app')

		const classes = [
			'max-h-screen',
			'w-full',
			'overflow-hidden',
			'fixed',
			'top-0',
			'left-0',
		]

		if (value) {
			appEl?.classList.add(...classes)
		} else {
			appEl?.classList.remove(...classes)
		}
	}
)
</script>

<style module>
.drawer {
	@apply flex flex-col flex-nowrap justify-between min-h-screen w-1/2 absolute top-0 right-0 z-20;
	background: hsl(247, 40%, 18%);
	box-shadow: -25px 0 50px -12px hsl(247, 40%, 8%);
}

.drawerMask {
	@apply h-screen w-screen fixed top-0 left-0 z-10;
	background: hsla(247, 40%, 4%, 0.5);
	backdrop-filter: blur(2px);
}

.closeButton {
	@apply rounded absolute right-5 top-5 z-10 p-2 hover:bg-theme-button-primary-hover transition-colors;
}
.closeIcon {
	@apply h-6;
}
</style>

<style>
.drawer-enter-active,
.drawer-leave-active {
	transition: all 0.3s ease-out;
}

.drawer-leave-active {
	transition: all 0.2s ease-in;
}

.drawer-enter-from,
.drawer-leave-to {
	transform: translateX(100%);
}

.drawer-enter-to,
.drawer-leave-from {
	transform: translateX(0);
}

.drawer-mask-enter-active {
	transition: all 0.3s ease-out;
}

.drawer-mask-leave-active {
	transition: all 0.3s ease-in;
}

.drawer-mask-enter-from,
.drawer-mask-leave-to {
	opacity: 0;
}

.drawer-mask-enter-to,
.drawer-mask-leave-from {
	opacity: 1;
}
</style>
