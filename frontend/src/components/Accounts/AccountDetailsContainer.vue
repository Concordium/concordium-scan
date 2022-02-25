<template>
	<div v-if="accountQueryResult.data">
		<AccountDetailsContent
			v-if="accountQueryResult.data.account"
			:account="accountQueryResult.data.account"
			:go-to-page="goToPage"
		/>
		<AccountDetailsContent
			v-else
			:account="accountQueryResult.data.accountByAddress"
			:go-to-page="goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import AccountDetailsContent from '~/components/Accounts/AccountDetailsContent.vue'
import {
	useAccountQuery,
	useAccountQueryByAddress,
} from '~/queries/useAccountQuery'
const { first, last, after, before, goToPage } = usePagination()

type Props = {
	id?: string
	address?: string
}
const props = defineProps<Props>()
const refId = toRef(props, 'id')
const refAddress = toRef(props, 'address')

const accountQueryResult = ref()
if (props.id)
	accountQueryResult.value = useAccountQuery(refId as Ref<string>, {
		first,
		last,
		after,
		before,
	})
else if (props.address)
	accountQueryResult.value = useAccountQueryByAddress(
		refAddress as Ref<string>,
		{
			first,
			last,
			after,
			before,
		}
	)
</script>
