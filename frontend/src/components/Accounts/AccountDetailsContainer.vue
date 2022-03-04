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
	<BWCubeLogoIcon
		v-else
		class="w-10 h-10 animate-ping absolute top-1/3 right-1/2"
	/>
</template>

<script lang="ts" setup>
import type { Ref } from 'vue'
import AccountDetailsContent from '~/components/Accounts/AccountDetailsContent.vue'
import {
	useAccountQuery,
	useAccountQueryByAddress,
} from '~/queries/useAccountQuery'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
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
