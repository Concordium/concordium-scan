<template>
	<TableHead>
		<TableRow>
			<TableTh>Address</TableTh>
			<TableTh align="right">Balance</TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="account in accounts" :key="account">
			<TableTd>
				<AccountLink :address="account.account.address.asString" />
			</TableTd>
			<TableTd align="right">
				<TokenAmount
					:amount="account.balance"
					:symbol="symbol"
					:fraction-digits="Number(decimals || 0)"
				/>
			</TableTd>
		</TableRow>
	</TableBody>
</template>
<script lang="ts" setup>
import TokenAmount from '../atoms/TokenAmount.vue'
import AccountLink from '../molecules/AccountLink.vue'
import { AccountToken } from '~~/src/types/generated'

type Props = {
	accounts: AccountToken[]
	decimals?: number | undefined
	symbol?: string | undefined
}
defineProps<Props>()
</script>
