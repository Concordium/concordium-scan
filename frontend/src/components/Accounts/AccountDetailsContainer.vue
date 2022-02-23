<template>
	<div v-if="accountQueryResult.data">
		<AccountDetailsContent
			v-if="accountQueryResult.data.account"
			:account="accountQueryResult.data.account"
		/>
		<AccountDetailsContent
			v-else
			:account="accountQueryResult.data.accountByAddress"
		/>
	</div>
</template>

<script lang="ts" setup>
import AccountDetailsContent from '~/components/Accounts/AccountDetailsContent.vue'
import {
	useAccountQuery,
	useAccountQueryByAddress,
} from '~/queries/useAccountQuery'

type Props = {
	id?: string
	address?: string
}

const props = defineProps<Props>()
const accountQueryResult = ref()
if (props.id) accountQueryResult.value = useAccountQuery(props.id)
else if (props.address)
	accountQueryResult.value = useAccountQueryByAddress(props.address)
</script>
