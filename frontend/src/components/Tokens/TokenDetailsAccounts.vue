<template>
	<TableHead>
		<TableRow>
			<TableTh></TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="account in accounts" :key="account">
			<TableTd>
				{{ account.account.address }}
			</TableTd>
		</TableRow>
	</TableBody>
</template>
<script lang="ts" setup>
import { AccountToken } from '~~/src/types/generated'

type Props = {
	accounts: AccountToken[]
}
</script>
