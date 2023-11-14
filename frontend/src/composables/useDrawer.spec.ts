import { ref } from 'vue'
import { useRouter } from 'vue-router'
import {
	isItemOnTop,
	pushToRouter,
	type DrawerItem,
	type DrawerList,
} from './useDrawer'

jest.mock('vue-router', () => ({
	useRouter: () => ({
		push: jest.fn(),
	}),
}))

describe('useDrawer', () => {
	describe('isItemOnTop', () => {
		it('should never match if there are no drawers currently', () => {
			const currentTopItem = ref(undefined)

			expect(
				isItemOnTop({ entityTypeName: 'block', id: '42' }, currentTopItem)
			).toBe(false)
		})

		it('should not match two different drawer types', () => {
			const currentTopItem = ref<DrawerItem>({
				entityTypeName: 'block',
				id: '1337',
			})

			expect(
				isItemOnTop(
					{ entityTypeName: 'transaction', id: '1337' },
					currentTopItem
				)
			).toBe(false)
		})

		describe('when entity is a block', () => {
			it('should match a block drawer by id', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'block',
					id: '1337',
				})

				expect(
					isItemOnTop({ entityTypeName: 'block', id: '42' }, currentTopItem)
				).toBe(false)
				expect(
					isItemOnTop({ entityTypeName: 'block', id: '1337' }, currentTopItem)
				).toBe(true)
			})

			it('should match a block drawer by hash', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'block',
					hash: '1337',
				})

				expect(
					isItemOnTop({ entityTypeName: 'block', hash: '42' }, currentTopItem)
				).toBe(false)
				expect(
					isItemOnTop({ entityTypeName: 'block', hash: '1337' }, currentTopItem)
				).toBe(true)
			})

			it('should not match a block with a missing id or hash', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'block',
					hash: '',
					id: undefined,
				})

				expect(
					isItemOnTop(
						{ entityTypeName: 'block', id: undefined, hash: '' },
						currentTopItem
					)
				).toBe(false)
			})
		})

		describe('when entity is a transaction', () => {
			it('should match a transaction drawer by id', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'transaction',
					id: '1337',
				})

				expect(
					isItemOnTop(
						{ entityTypeName: 'transaction', id: '42' },
						currentTopItem
					)
				).toBe(false)
				expect(
					isItemOnTop(
						{ entityTypeName: 'transaction', id: '1337' },
						currentTopItem
					)
				).toBe(true)
			})

			it('should match a transaction drawer by hash', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'transaction',
					hash: '1337',
				})

				expect(
					isItemOnTop(
						{ entityTypeName: 'transaction', hash: '42' },
						currentTopItem
					)
				).toBe(false)
				expect(
					isItemOnTop(
						{ entityTypeName: 'transaction', hash: '1337' },
						currentTopItem
					)
				).toBe(true)
			})

			it('should not match a transaction with a missing id or hash', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'block',
					hash: '',
					id: undefined,
				})

				expect(
					isItemOnTop(
						{ entityTypeName: 'transaction', id: undefined, hash: '' },
						currentTopItem
					)
				).toBe(false)
			})
		})

		describe('when entity is an account', () => {
			it('should match a account drawer by id', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'account',
					id: '1337',
				})

				expect(
					isItemOnTop({ entityTypeName: 'account', id: '42' }, currentTopItem)
				).toBe(false)
				expect(
					isItemOnTop({ entityTypeName: 'account', id: '1337' }, currentTopItem)
				).toBe(true)
			})

			it('should match an account drawer by address', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'account',
					address: '1337',
				})

				expect(
					isItemOnTop(
						{ entityTypeName: 'account', address: '42' },
						currentTopItem
					)
				).toBe(false)
				expect(
					isItemOnTop(
						{ entityTypeName: 'account', address: '1337' },
						currentTopItem
					)
				).toBe(true)
			})

			it('should not match an account with a missing id or address', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'account',
					address: '',
					id: undefined,
				})

				expect(
					isItemOnTop(
						{ entityTypeName: 'account', id: undefined, address: '' },
						currentTopItem
					)
				).toBe(false)
			})
		})

		describe('when entity is a baker', () => {
			it('should match a baker drawer by bakerId', () => {
				const currentTopItem = ref<DrawerItem>({
					entityTypeName: 'baker',
					id: 666,
				})

				expect(
					isItemOnTop({ entityTypeName: 'baker', id: 42 }, currentTopItem)
				).toBe(false)
				expect(
					isItemOnTop({ entityTypeName: 'baker', id: 666 }, currentTopItem)
				).toBe(true)
			})
		})
	})

	describe('pushToRouter', () => {
		it('will push the total count of drawers by default', () => {
			const router = useRouter()
			const pushSpy = jest.spyOn(router, 'push')

			const items = [
				{
					entityTypeName: 'block',
					hash: 'h3ll0',
				},
				{
					entityTypeName: 'transaction',
					hash: 'w0r1d',
				},
			] as DrawerItem[]

			const state = ref<DrawerList>({ items })

			pushToRouter(items[0])(router, state)

			expect(pushSpy).toHaveBeenCalledWith({
				query: {
					daddress: undefined,
					dcount: 2,
					dentity: 'block',
					dhash: 'h3ll0',
					did: undefined,
				},
			})
		})

		it('can count only the last item as an option', () => {
			const router = useRouter()
			const pushSpy = jest.spyOn(router, 'push')

			const items = [
				{
					entityTypeName: 'block',
					hash: 'h3ll0',
				},
				{
					entityTypeName: 'transaction',
					hash: 'w0r1d',
				},
			] as DrawerItem[]

			const state = ref<DrawerList>({ items })

			pushToRouter(items[0], false)(router, state)

			expect(pushSpy).toHaveBeenCalledWith({
				query: {
					daddress: undefined,
					dcount: 1,
					dentity: 'block',
					dhash: 'h3ll0',
					did: undefined,
				},
			})
		})

		it('should push a block to the route', () => {
			const router = useRouter()
			const pushSpy = jest.spyOn(router, 'push')

			const item = {
				entityTypeName: 'block',
				hash: 'b4da55',
			} as DrawerItem

			const state = ref<DrawerList>({ items: [item] })

			pushToRouter(item)(router, state)

			expect(pushSpy).toHaveBeenCalledWith({
				query: {
					daddress: undefined,
					dcount: 1,
					dentity: 'block',
					dhash: 'b4da55',
					did: undefined,
				},
			})
		})

		it('should push a transaction to the route', () => {
			const router = useRouter()
			const pushSpy = jest.spyOn(router, 'push')

			const item = {
				entityTypeName: 'transaction',
				hash: 'b4da55',
			} as DrawerItem

			const state = ref<DrawerList>({ items: [item] })

			pushToRouter(item)(router, state)

			expect(pushSpy).toHaveBeenCalledWith({
				query: {
					daddress: undefined,
					dcount: 1,
					dentity: 'transaction',
					dhash: 'b4da55',
					did: undefined,
				},
			})
		})

		it('should push an account to the route', () => {
			const router = useRouter()
			const pushSpy = jest.spyOn(router, 'push')

			const item = {
				entityTypeName: 'account',
				address: '1337-add-355',
			} as DrawerItem

			const state = ref<DrawerList>({ items: [item] })

			pushToRouter(item)(router, state)

			expect(pushSpy).toHaveBeenCalledWith({
				query: {
					daddress: '1337-add-355',
					dcount: 1,
					dentity: 'account',
					dhash: undefined,
					did: undefined,
				},
			})
		})

		it('should push a baker to the route', () => {
			const router = useRouter()
			const pushSpy = jest.spyOn(router, 'push')

			const item = {
				entityTypeName: 'baker',
				id: 123,
			} as DrawerItem

			const state = ref<DrawerList>({ items: [item] })

			pushToRouter(item)(router, state)

			expect(pushSpy).toHaveBeenCalledWith({
				query: {
					daddress: undefined,
					dcount: 1,
					dentity: 'baker',
					dhash: undefined,
					did: 123,
				},
			})
		})
	})
})
