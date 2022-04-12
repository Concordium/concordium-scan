import { ref } from 'vue'
import { isItemOnTop, type DrawerItem } from './useDrawer'

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
	})
})
