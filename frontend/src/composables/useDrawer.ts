// eslint-disable-next-line @typescript-eslint/ban-ts-comment
// @ts-ignore : This alias exists, but tsc doesn't see it
import { useState } from '#app'
type DrawerItem = {
	entityTypeName: string
	hash: string
	id: string
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
	if (drawerState.value.items.length < parseInt(route.query.dcount as string)) {
		router.push({ query: {} })
	}
	// Beware this watch is set up on all components that uses this composable. TODO: rewrite?
	watch(route, to => {
		if (to.query.dcount) {
			const dcountAsInt = parseInt(to.query.dcount as string)
			// This makes sure we only run the underlying set once. In case this would do more.
			if (currentDrawerCount.value === dcountAsInt) return
			currentDrawerCount.value = dcountAsInt
		} else {
			softReset()
		}
	})
	// Soft reset of counter, which closes to drawer, but can still be navigated to with "forward"-button on mouse.
	const softReset = () => {
		currentDrawerCount.value = 0
	}
	// 'Hard' reset of both the counter and the data. Which is used when we need to discard the old drawer
	const reset = () => {
		drawerState.value.items = []
		currentDrawerCount.value = 0
	}
	const currentTopItem = computed(() => {
		if (drawerState && drawerState.value && drawerState.value.items.length > 0)
			return drawerState.value.items[currentDrawerCount.value - 1]
		return undefined
	})

	const push = (entityTypeName: string, hash: string, id: string) => {
		const item = { entityTypeName, hash, id }
		if (currentDrawerCount.value === 0) reset()
		drawerState.value.items.push(item)
		router.push({ query: { dcount: drawerState.value.items.length } })
	}
	const getItems = () => {
		return drawerState.value.items
	}
	return { push, getItems, currentTopItem }
}
