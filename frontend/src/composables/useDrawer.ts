// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import { RouteLocationNormalizedLoaded } from 'vue-router'
import { useState } from '#app'
type DrawerItem = {
	entityTypeName: string
	hash?: string
	id?: string
	address?: string
	scrollY?: number
}
type DrawerList = {
	items: DrawerItem[]
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
		if (route.query.dentity && (route.query.dhash || route.query.daddress)) {
			push(
				route.query.dentity as string,
				route.query.dhash as string,
				undefined,
				route.query.daddress as string,
				false
			)
		} else router.push({ query: {} })
		//	}
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

	const push = (
		entityTypeName: string,
		hash?: string,
		id?: string,
		address?: string,
		resetList = true
	) => {
		if (
			currentTopItem.value &&
			currentTopItem.value.entityTypeName === entityTypeName &&
			((currentTopItem.value.hash !== null &&
				(hash !== undefined || null) &&
				currentTopItem.value.hash === hash) ||
				(currentTopItem.value.id !== null &&
					(id !== undefined || null) &&
					currentTopItem.value.id === id) ||
				(currentTopItem.value.address !== null &&
					(address !== undefined || null) &&
					currentTopItem.value.address === address))
		) {
			return
		}
		let scrollY = 0
		if (process.client) {
			scrollY = window.scrollY
		}
		const item = {
			entityTypeName,
			hash,
			id,
			address,
			scrollY,
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
		router.push({
			query: {
				dcount: resetList ? drawerState.value.items.length : 1,
				dentity: entityTypeName,
				dhash: hash,
				daddress: address,
			},
		})
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
