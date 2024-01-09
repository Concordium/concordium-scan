<template>
	<div>
		<transition name="drawer-mask">
			<div
				v-if="currentDrawerCount > 0"
				class="h-screen w-screen fixed top-0 left-0 z-20"
				:class="$style.drawerMask"
				@click="() => softReset()"
			></div>
		</transition>
		<TransitionGroup name="drawer">
			<div
				v-for="(drawerItem, index) in getDisplayItems()"
				:key="index"
				class="flex-col flex-nowrap justify-between min-h-screen w-full absolute top-0 right-0 z-20 overflow-x-hidden"
				:class="[
					$style.drawer,
					$style.fixedAndMaxHeight,
					$style['drawertranslate-x-' + (currentDrawerCount - 1 - index)],
					currentDrawerCount - 1 === index ? $style.drawerItemActive : '',
					index < currentDrawerCount - 2 ? 'hidden md:flex' : 'flex',
				]"
			>
				<Drawer :is-open="currentDrawerCount > -1">
					<template #content>
						<BlockDetailsContainer
							v-if="drawerItem && drawerItem.entityTypeName === 'block'"
							:id="drawerItem.id"
							:hash="drawerItem.hash"
						/>
						<TransactionDetailsContainer
							v-else-if="
								drawerItem && drawerItem.entityTypeName === 'transaction'
							"
							:id="drawerItem.id"
							:hash="drawerItem.hash"
						/>
						<AccountDetailsContainer
							v-else-if="drawerItem && drawerItem.entityTypeName === 'account'"
							:id="drawerItem.id"
							:address="drawerItem.address"
						/>
						<ContractDetailsContainer
							v-else-if="drawerItem && drawerItem.entityTypeName === 'contract'"
							:id="drawerItem.contractAddressIndex"
							:contract-address-index="drawerItem.contractAddressIndex"
							:contract-address-sub-index="drawerItem.contractAddressSubIndex"
						/>
						<ModuleDetailsContainer
							v-else-if="drawerItem && drawerItem.entityTypeName === 'module'"
							:module-reference="drawerItem.moduleReference"
						/>
						<BakerDetailsContainer
							v-else-if="
								drawerItem && drawerItem.entityTypeName === 'validator'
							"
							:baker-id="drawerItem.id"
						/>
						<PassiveDelegationContainer
							v-else-if="
								drawerItem && drawerItem.entityTypeName === 'passiveDelegation'
							"
						/>
						<NodeDetailsContainer
							v-else-if="drawerItem && drawerItem.entityTypeName === 'node'"
							:node-internal-id="drawerItem.nodeId"
						/>
						<TokenDetailsContainer
							v-else-if="drawerItem && drawerItem.entityTypeName === 'token'"
							:token-id="drawerItem.tokenId"
							:contract-address-index="drawerItem.contractAddressIndex"
							:contract-address-sub-index="drawerItem.contractAddressSubIndex"
						/>
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
import BakerDetailsContainer from '~/components/BakerDetails/BakerDetailsContainer.vue'
import PassiveDelegationContainer from '~/components/PassiveDelegation/PassiveDelegationContainer.vue'
import NodeDetailsContainer from '~/components/NodeDetails/NodeDetailsContainer.vue'
import ContractDetailsContainer from '~/components/Contracts/ContractDetailsContainer.vue'
import ModuleDetailsContainer from '~/components/Module/ModuleDetailsContainer.vue'
import TokenDetailsContainer from '~/components/Tokens/TokenDetailsContainer.vue'

const { softReset, getDisplayItems, currentDrawerCount, currentTopItem } =
	useDrawer()

watch(currentTopItem, () => {
	if (currentTopItem && currentTopItem.value) {
		window.scrollTo(0, currentTopItem.value.scrollY ?? 0)
	}
})
</script>
<style module>
.drawerMask {
	background: hsla(247, 40%, 4%, 0.5);
	backdrop-filter: blur(2px);
}

.drawer {
	max-width: 1440px;
	background: hsl(247, 40%, 18%);
	box-shadow: -25px 0 50px -12px var(--color-shadow-dark);
	transition: 0.3s ease-in-out;
}
@media screen and (max-width: 960px) {
	.drawer {
		max-width: 100%;
	}
}

.fixedAndMaxHeight {
	max-height: 100vh;
	position: fixed;
}
.drawertranslate-x-0 {
	z-index: 50;
}
.drawertranslate-x-1 {
	transform: translate3d(-20px, 0, 0);
	pointer-events: none;
	z-index: 49;
}
.drawertranslate-x-2 {
	transform: translate3d(-40px, 0, 0);
	pointer-events: none;
	z-index: 48;
}
.drawertranslate-x-3 {
	transform: translate3d(-60px, 0, 0);
	pointer-events: none;
	z-index: 47;
}
.drawertranslate-x-4 {
	transform: translate3d(-80px, 0, 0);
	pointer-events: none;
	z-index: 46;
}
.drawertranslate-x-5 {
	transform: translate3d(-100px, 0, 0);
	pointer-events: none;
	z-index: 45;
}
.drawertranslate-x-6 {
	transform: translate3d(-120px, 0, 0);
	pointer-events: none;
	z-index: 44;
}
.drawertranslate-x-7 {
	transform: translate3d(-140px, 0, 0);
	pointer-events: none;
	z-index: 43;
}
.drawertranslate-x-8 {
	transform: translate3d(-160px, 0, 0);
	z-index: 42;
	pointer-events: none;
}
.drawertranslate-x-9 {
	transform: translate3d(-180px, 0, 0);
	pointer-events: none;
	z-index: 41;
}
.drawertranslate-x-10 {
	transform: translate3d(-200px, 0, 0);
	pointer-events: none;
	z-index: 40;
}
.drawertranslate-x-11 {
	transform: translate3d(-220px, 0, 0);
	pointer-events: none;
	z-index: 39;
}
.drawertranslate-x-12 {
	transform: translate3d(-240px, 0, 0);
	pointer-events: none;
	z-index: 38;
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
