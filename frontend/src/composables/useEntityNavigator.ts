export enum EntityType {
	Unknown = 0,
	Transaction = 'transaction',
	Block = 'block',
	Account = 'account',
}
export const useEntityNavigator = () => {
	const goto = (
		entity: EntityType,
		id: string | undefined,
		hash: string | undefined
	) => {
		const routeName = ref('')
		const routeParams = ref({})
		const router = useRouter()
		switch (entity) {
			case EntityType.Transaction:
				routeName.value = 'transactions-transactionHash'
				routeParams.value = { internalId: id, transactionHash: hash }
				break
			case EntityType.Block:
				routeName.value = 'blocks-blockHash'
				routeParams.value = { internalId: id, blockHash: hash }
				break
			case EntityType.Account:
				routeName.value = 'accounts-accountHash'
				routeParams.value = { internalId: id, accountHash: hash }
				break
		}
		router.push({ name: routeName.value, params: routeParams.value })
	}
	const getEntityTypeByString = (entityName: string) => {
		switch (entityName.toLowerCase()) {
			case 'transaction':
				return EntityType.Transaction
			case 'block':
				return EntityType.Block
			case 'account':
				return EntityType.Account
		}
		return EntityType.Unknown
	}
	return { goto, getEntityTypeByString }
}
