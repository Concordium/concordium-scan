// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import { Ref } from 'vue'
import {
	useRouter,
	RouteLocationNormalizedLoaded,
	type Router,
} from 'vue-router'
import { useState } from '#app'

type BlockDrawerItem = {
	entityTypeName: 'block'
} & ({ id: string; hash?: string } | { hash: string; id?: string })

type TxDrawerItem = {
	entityTypeName: 'transaction'
} & ({ id: string; hash?: string } | { hash: string; id?: string })

type AccountDrawerItem = {
	entityTypeName: 'account'
} & ({ id: string; address?: string } | { address: string; id?: string })

type BakerDrawerItem = {
	entityTypeName: 'baker'
	bakerId: number
}
type PassiveDelegationItem = {
	entityTypeName: 'passiveDelegation'
}
export type DrawerItem = (
	| BlockDrawerItem
	| TxDrawerItem
	| AccountDrawerItem
	| BakerDrawerItem
	| PassiveDelegationItem
) & {
	scrollY?: number
}

export type DrawerList = {
	items: DrawerItem[]
}

/**
 * Function to determine whether an item is on top of the stack of drawers
 */
export const isItemOnTop = (
	item: DrawerItem,
	currentTopItem: Ref<DrawerItem | undefined>
): boolean => {
	if (
		!currentTopItem.value ||
		item.entityTypeName !== currentTopItem.value.entityTypeName
	)
		return false

	if (
		(item.entityTypeName === 'block' ||
			item.entityTypeName === 'transaction') &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(
			(item.hash && item.hash === currentTopItem.value.hash) ||
			(item.id && item.id === currentTopItem.value.id)
		)

	if (
		item.entityTypeName === 'account' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(
			(item.address && item.address === currentTopItem.value.address) ||
			(item.id && item.id === currentTopItem.value.id)
		)

	if (
		item.entityTypeName === 'baker' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	) {
		return !!(item.bakerId && item.bakerId === currentTopItem.value.bakerId)
	}

	if (
		item.entityTypeName === 'passiveDelegation' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return true

	return false
}

/**
 * Curried function to add a new item to the drawer stack in router history.
 * @param { DrawerItem } drawerItem  - item to be pushed to the router
 * @param { boolean } resetList - whether or not list should be reset
 * @param { Router } router - (in returned fn) instance of vue-router
 * @param { Ref<DrawerList> } state - (in returned fn) state containing list of items in drawer
 */
export const pushToRouter =
	(drawerItem: DrawerItem, resetList = true) =>
	(router: Router, state: Ref<DrawerList>) => {
		router.push({
			query: {
				dcount: resetList ? state.value.items.length : 1,
				dentity: drawerItem.entityTypeName,
				daddress:
					drawerItem.entityTypeName === 'account'
						? drawerItem.address
						: undefined,
				dhash:
					drawerItem.entityTypeName === 'block' ||
					drawerItem.entityTypeName === 'transaction'
						? drawerItem.hash
						: undefined,
				did:
					drawerItem.entityTypeName === 'baker'
						? drawerItem.bakerId
						: undefined,
			},
		})
	}

export const useDrawer = () => {
	const drawerState = useState<DrawerList>('drawerItems', () => {
		return {
			items: [],
		}
	})
	const currentDrawerCount = useState<number>('currentDrawerCount', () => 0)
	const router = useRouter()

	const handleInitialLoad = (route: RouteLocationNormalizedLoaded) => {
		if (route.query.dentity === 'account' && route.query.daddress) {
			push(
				{
					entityTypeName: 'account',
					address: route.query.daddress as string,
				},
				false
			)
		} else if (route.query.dentity === 'block' && route.query.dhash) {
			push(
				{
					entityTypeName: 'block',
					hash: route.query.dhash as string,
				},
				false
			)
		} else if (route.query.dentity === 'transaction' && route.query.dhash) {
			push(
				{
					entityTypeName: 'transaction',
					hash: route.query.dhash as string,
				},
				false
			)
		} else if (route.query.dentity === 'baker' && route.query.did) {
			push(
				{
					entityTypeName: 'baker',
					bakerId: parseInt(route.query.did.toString()),
				},
				false
			)
		} else if (route.query.dentity === 'passiveDelegation') {
			push(
				{
					entityTypeName: 'passiveDelegation',
				},
				false
			)
		} else router.push({ query: {} })
	}

	const updateByRouteData = (route: RouteLocationNormalizedLoaded) => {
		if (route.query.dcount) {
			const dcountAsInt = parseInt(route.query.dcount as string)
			currentDrawerCount.value = dcountAsInt
		} else {
			softReset()
		}
	}
	// Soft reset of counter, which closes the drawer, but can still be navigated to with "forward"-button on mouse.
	const softReset = () => {
		router.push({ query: {} })
		currentDrawerCount.value = 0
	}
	// 'Hard' reset of both the counter and the data. Which is used when we need to discard the old drawer
	const reset = () => {
		drawerState.value.items = []
		currentDrawerCount.value = 0
	}
	const currentTopItem = computed(() => {
		return drawerState?.value?.items[currentDrawerCount.value - 1]
	})

	const push = (drawerItem: DrawerItem, resetList = true) => {
		if (isItemOnTop(drawerItem, currentTopItem)) {
			pushToRouter(drawerItem, resetList)(router, drawerState)
			return
		}

		const item = {
			...drawerItem,
			scrollY: process.client ? window.scrollY : 0,
		}

		if (currentDrawerCount.value === 0 && resetList) {
			reset()
		}
		drawerState.value.items = drawerState.value.items.slice(
			0,
			currentDrawerCount.value
		)
		drawerState.value.items.push(item)
		currentDrawerCount.value = resetList ? drawerState.value.items.length : 1

		pushToRouter(drawerItem, resetList)(router, drawerState)
	}
	const getItems = () => {
		return drawerState.value.items
	}
	const getDisplayItems = () => {
		return drawerState.value.items.slice(0, currentDrawerCount.value)
	}

	const canGoForward = computed(() => {
		return getItems().indexOf(currentTopItem.value) !== getItems().length - 1
	})
	return {
		push,
		getItems,
		currentTopItem,
		softReset,
		updateByRouteData,
		handleInitialLoad,
		canGoForward,
		getDisplayItems,
		currentDrawerCount,
	}
}
