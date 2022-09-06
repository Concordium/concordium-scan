<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh v-if="breakpoint >= Breakpoint.LG">Index</TableTh>
					<TableTh>Token Id</TableTh>
					<TableTh>Balance</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="token in tokens"
					:key="token.contractIndex + token.contractSubIndex + token.tokenId"
				>
					<TableTd v-if="breakpoint >= Breakpoint.LG">
						{{ token.contractIndex }} / {{ token.contractSubIndex }}
					</TableTd>
					<TableTd>
						<TokenLink
							:token-id="token.tokenId"
							:url="token.token.metadataUrl"
						/>
					</TableTd>
					<TableTd>
						{{ token.balance }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</div>
</template>
<script lang="ts" setup>
import { AccountToken } from '~~/src/types/generated.js'
import { useBreakpoint, Breakpoint } from '~/composables/useBreakpoint'

const { breakpoint } = useBreakpoint()

type Props = {
	tokens: AccountToken[]
}

defineProps<Props>()
</script>
