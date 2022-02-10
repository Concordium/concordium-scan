// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import { RouteLocationNormalizedLoaded } from 'vue-router'
import { useState } from '#app'
type DrawerItem = {
	entityTypeName: string
	hash: string
	id?: string
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
	const route = useRoute()

	// Beware this watch is set up on all components that uses this composable. TODO: rewrite?
	const handleWatch = (to: RouteLocationNormalizedLoaded) => {
		if (to.query.dcount) {
			const dcountAsInt = parseInt(to.query.dcount as string)
			// This makes sure we only run the underlying set once. In case this would do more.
			if (currentDrawerCount.value === dcountAsInt) return
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
	const currentTopItem = computed(
		() => drawerState?.value?.items[currentDrawerCount.value - 1]
	)

	const push = (entityTypeName: string, hash: string, id?: string) => {
		const item = { entityTypeName, hash, id }
		if (currentDrawerCount.value === 0) reset()
		drawerState.value.items.push(item)
		router.push({
			query: {
				dcount: drawerState.value.items.length,
				dentity: entityTypeName,
				dhash: hash,
			},
		})
	}
	const getItems = () => {
		return drawerState.value.items
	}
	if (drawerState.value.items.length < parseInt(route.query.dcount as string)) {
		if (route.query.dentity && route.query.dhash) {
			push(
				route.query.dentity as string,
				route.query.dhash as string,
				undefined
			)
		} else router.push({ query: {} })
	}

	return { push, getItems, currentTopItem, softReset, handleWatch }
}
