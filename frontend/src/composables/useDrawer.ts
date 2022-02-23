// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import { RouteLocationNormalizedLoaded } from 'vue-router'
import { useState } from '#app'
type DrawerItem = {
	entityTypeName: string
	hash?: string
	id?: string
	address?: string
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
		const item = { entityTypeName, hash, id, address }

		if (currentDrawerCount.value === 0 && resetList) {
			reset()
		} else {
			currentDrawerCount.value = 1
		}
		drawerState.value.items.push(item)
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

	return {
		push,
		getItems,
		currentTopItem,
		softReset,
		updateByRouteData,
		handleInitialLoad,
	}
}
