<template>
	<LinkButton
		:on-click="
			() =>
				$router.push({
					name: entityRoute,
					params: entityParams,
				})
		"
	>
		<slot />
	</LinkButton>
</template>

<script lang="ts" setup>
type Props = {
	entity: string
	id?: string
	hash?: string
}

const props = defineProps<Props>()
const entityRoute = ref('')
const entityParams = ref({
	internalId: props.id,
})
switch (props.entity) {
	case 'transaction':
		entityRoute.value = 'transactions-transactionHash'
		entityParams.value = { ...entityParams.value, transactionHash: props.hash }
		break
	case 'block':
		entityRoute.value = 'blocks-blockHash'
		entityParams.value = { ...entityParams.value, blockHash: props.hash }
		break
	case 'account':
		entityRoute.value = 'accounts-accountHash'
		entityParams.value = { ...entityParams.value, accountHash: props.hash }
		break
}
</script>
