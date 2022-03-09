<template>
	<div>
		<transition name="drawer-mask">
			<div
				v-if="currentDrawerCount > 0"
				:class="$style.drawerMask"
				style="z-index: 1"
				@click="() => softReset()"
			></div>
		</transition>
		<TransitionGroup name="drawer">
			<div
				v-for="(drawerItem, index) in getDisplayItems()"
				:key="drawerItem"
				:class="$style.drawer"
				class="relative"
				:style="[
					'z-index:' + (index == currentDrawerCount - 1 ? 2 : 1),
					'max-height:' + (index == currentDrawerCount - 1 ? '' : '100vh'),
					'position:' +
						(index == currentDrawerCount - 1 ? 'absolute' : 'fixed'),
					currentDrawerCount - 1 - index == 0
						? ''
						: 'transform:' +
						  'translateX(-' +
						  (currentDrawerCount - 1 - index) * 20 +
						  'px)',
				]"
			>
				<Drawer :is-open="currentDrawerCount > 0" :on-close="() => softReset()">
					<template #content>
						<BlockDetailsContainer
							v-if="drawerItem && drawerItem.entityTypeName == 'block'"
							:id="drawerItem?.id"
							:hash="drawerItem?.hash"
						/>
						<TransactionDetailsContainer
							v-if="drawerItem && drawerItem.entityTypeName == 'transaction'"
							:id="drawerItem?.id"
							:hash="drawerItem?.hash"
						/>
						<AccountDetailsContainer
							v-if="drawerItem && drawerItem.entityTypeName == 'account'"
							:id="drawerItem?.id"
							:address="drawerItem?.address"
						/>
					</template>

					<template #actions>
						<DrawerActions class="flex flex-grow-0 justify-between">
							<div class="flex gap-4">
								<Button class="self-start" @click="back">
									<ChevronBackIcon class="align-text-top" /> Back
								</Button>
								<Button v-if="canGoForward" class="self-start" @click="forward">
									Forward <ChevronForwardIcon class="align-text-top" />
								</Button>
							</div>
							<Button class="self-end" :on-click="() => softReset()">
								Close
							</Button>
						</DrawerActions>
					</template>
				</Drawer>
			</div>
		</TransitionGroup>
	</div>
</template>

<script lang="ts" setup>
import { useDrawer } from '~/composables/useDrawer'
import Drawer from '~/components/Drawer/Drawer.vue'
import AccountDetailsContainer from '~/components/Accounts/AccountDetailsContainer.vue'
import TransactionDetailsContainer from '~/components/TransactionDetails/TransactionDetailsContainer.vue'
import BlockDetailsContainer from '~/components/BlockDetails/BlockDetailsContainer.vue'
import ChevronBackIcon from '~/components/icons/ChevronBackIcon.vue'
import ChevronForwardIcon from '~/components/icons/ChevronForwardIcon.vue'
const {
	softReset,
	currentDepth,
	canGoForward,
	getDisplayItems,
	currentDrawerCount,
	currentTopItem,
} = useDrawer()
const router = useRouter()
const back = () => {
	// Depth is only 1 if it was a direct link to the drawer
	if (currentDepth() > 1) router.go(-1)
	else softReset()
}
const forward = () => {
	if (canGoForward) router.go(1)
}
watch(currentTopItem, () => {
	if (currentTopItem && currentTopItem.value) {
		window.scrollTo(0, currentTopItem.value.scrollY ?? 0)
	}
})
</script>
<style module>
.drawerMask {
	@apply h-screen w-screen fixed top-0 left-0 z-10;
	background: hsla(247, 40%, 4%, 0.5);
	backdrop-filter: blur(2px);
}
.drawer {
	@apply flex flex-col flex-nowrap justify-between min-h-screen w-full md:w-3/4 xl:w-1/2 absolute top-0 right-0 z-20 overflow-x-hidden;
	background: hsl(247, 40%, 18%);
	box-shadow: -25px 0 50px -12px var(--color-shadow-dark);
	transition: 0.3s ease-in-out;
	-webkit-transition: 0.3s ease-in-out;
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
