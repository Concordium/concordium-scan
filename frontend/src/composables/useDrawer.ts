// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import type { Ref } from 'vue'
import {
	useRouter,
	type RouteLocationNormalizedLoaded,
	type Router,
} from 'vue-router'
import { useState } from '#app'

type BlockDrawerItem = {
	entityTypeName: 'block'
	hash: string
}

type TxDrawerItem = {
	entityTypeName: 'transaction'
} & ({ id: string; hash?: string } | { hash: string; id?: string })

type AccountDrawerItem = {
	entityTypeName: 'account'
} & ({ id: string; address?: string } | { address: string; id?: string })

type ContractDrawerItem = {
	entityTypeName: 'contract'
	contractAddressIndex: number
	contractAddressSubIndex: number
}

type ModuleDrawerItem = {
	entityTypeName: 'module'
	moduleReference: string
}

/**
 * This type is kept such that links like
 * https://ccdscan.io/staking?dcount=1&dentity=baker&did=SOME_ID stills works after
 * tokenomic change from 'baker' to 'validator'.
 */
type BakerDrawerItem = {
	entityTypeName: 'baker'
	id: number
}

type ValidatorDrawerItem = {
	entityTypeName: 'validator'
	id: number
}

type NodeDrawerItem = {
	entityTypeName: 'node'
	nodeId: string
}
type PassiveDelegationItem = {
	entityTypeName: 'passiveDelegation'
}

type TokenDrawerItem = {
	entityTypeName: 'token'
	tokenId: string
	contractAddressIndex: number
	contractAddressSubIndex: number
}

export type DrawerItem = (
	| BlockDrawerItem
	| TxDrawerItem
	| AccountDrawerItem
	| ContractDrawerItem
	| ModuleDrawerItem
	| ValidatorDrawerItem
	| BakerDrawerItem
	| PassiveDelegationItem
	| NodeDrawerItem
	| TokenDrawerItem
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
		item.entityTypeName === 'transaction' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(
			(item.hash && item.hash === currentTopItem.value.hash) ||
			(item.id && item.id === currentTopItem.value.id)
		)
	if (
		item.entityTypeName === 'block' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(item.hash && item.hash === currentTopItem.value.hash)

	if (
		item.entityTypeName === 'account' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(
			(item.address && item.address === currentTopItem.value.address) ||
			(item.id && item.id === currentTopItem.value.id)
		)
	if (
		item.entityTypeName === 'contract' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(
			item.contractAddressIndex !== null &&
			item.contractAddressIndex !== undefined &&
			item.contractAddressSubIndex !== null &&
			item.contractAddressSubIndex !== undefined &&
			item.contractAddressIndex === currentTopItem.value.contractAddressIndex &&
			item.contractAddressSubIndex ===
				currentTopItem.value.contractAddressSubIndex
		)
	if (
		item.entityTypeName === 'module' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	)
		return !!(
			item.moduleReference &&
			item.moduleReference === currentTopItem.value.moduleReference
		)
	if (
		item.entityTypeName === 'validator' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	) {
		return !!(item.id && item.id === currentTopItem.value.id)
	}
	if (
		item.entityTypeName === 'node' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	) {
		return !!(item.nodeId && item.nodeId === currentTopItem.value.nodeId)
	}
	if (
		item.entityTypeName === 'token' &&
		item.entityTypeName === currentTopItem.value.entityTypeName
	) {
		return !!(
			item.contractAddressIndex !== null &&
			item.contractAddressSubIndex !== null &&
			item.contractAddressIndex !== undefined &&
			item.contractAddressSubIndex !== undefined &&
			item.tokenId === currentTopItem.value.tokenId &&
			item.contractAddressIndex === currentTopItem.value.contractAddressIndex &&
			item.contractAddressSubIndex ===
				currentTopItem.value.contractAddressSubIndex
		)
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
 * @param drawerItem  - item to be pushed to the router
 * @param resetList - whether or not list should be reset
 */
export const pushToRouter =
	(drawerItem: DrawerItem, resetList = true) =>
	/**
	 * Add the curried item to the drawer stack in router history.
	 * @param router - (in returned fn) instance of vue-router
	 * @param state - (in returned fn) state containing list of items in drawer
	 */
	(router: Router, state: Ref<DrawerList>) => {
		const dcount = resetList ? state.value.items.length : 1
		const dentity = drawerItem.entityTypeName

		switch (dentity) {
			case 'block':
			case 'transaction':
				router.push({
					query: {
						dcount,
						dentity,
						dhash: drawerItem.hash ?? undefined,
					},
				})
				break
			case 'account':
				router.push({
					query: {
						dcount,
						dentity,
						daddress: drawerItem.address ?? undefined,
					},
				})
				break
			case 'validator':
				router.push({
					query: {
						dcount,
						dentity,
						did: drawerItem.id ?? undefined,
					},
				})
				break
			case 'node':
				router.push({
					query: {
						dcount,
						dentity,
						did: encodeURIComponent(drawerItem.nodeId) ?? undefined,
					},
				})
				break
			case 'contract':
				router.push({
					query: {
						dcount,
						dentity,
						dcontractAddressIndex: drawerItem.contractAddressIndex ?? undefined,
						dcontractAddressSubIndex:
							drawerItem.contractAddressSubIndex ?? undefined,
					},
				})
				break
			case 'module':
				router.push({
					query: {
						dcount,
						dentity,
						dmoduleReference: drawerItem.moduleReference ?? undefined,
					},
				})
				break
			case 'token':
				router.push({
					query: {
						dcount,
						dentity,
						did: drawerItem.tokenId ?? undefined,
						dcontractAddressIndex: drawerItem.contractAddressIndex ?? undefined,
						dcontractAddressSubIndex:
							drawerItem.contractAddressSubIndex ?? undefined,
					},
				})
				break
		}
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
		} else if (
			route.query.dentity === 'module' &&
			route.query.dmoduleReference
		) {
			push(
				{
					entityTypeName: 'module',
					moduleReference: route.query.dmoduleReference as string,
				},
				false
			)
		} else if (
			route.query.dentity === 'contract' &&
			route.query.dcontractAddressIndex &&
			route.query.dcontractAddressSubIndex
		) {
			push(
				{
					entityTypeName: 'contract',
					contractAddressIndex: parseInt(
						route.query.dcontractAddressIndex as string
					),
					contractAddressSubIndex: parseInt(
						route.query.dcontractAddressSubIndex as string
					),
				},
				false
			)
		} else if (
			(route.query.dentity === 'validator' ||
				route.query.dentity === 'baker') &&
			route.query.did
		) {
			push(
				{
					entityTypeName: 'validator',
					id: parseInt(route.query.did.toString()),
				},
				false
			)
		} else if (route.query.dentity === 'node' && route.query.did) {
			push(
				{
					entityTypeName: 'node',
					nodeId: decodeURIComponent(route.query.did.toString()),
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
		} else if (route.query.dentity === 'token') {
			push({
				entityTypeName: 'token',
				tokenId: route.query.did as string,
				contractAddressIndex: parseInt(
					route.query.dcontractAddressIndex as string
				),
				contractAddressSubIndex: parseInt(
					route.query.dcontractAddressSubIndex as string
				),
			})
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
			scrollY: import.meta.client ? window.scrollY : 0,
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
