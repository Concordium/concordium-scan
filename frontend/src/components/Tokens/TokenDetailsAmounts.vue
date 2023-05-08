<template>
	<DetailsCard>
		<template #title
			>Supply {{ !!metadata?.symbol ? metadata?.symbol : '' }}</template
		>
		<template #default>
			<TokenAmount :amount="token.totalSupply" data-testid="total-balance" />
		</template>
	</DetailsCard>
</template>

<script lang="ts" setup>
import TokenAmount from '../atoms/TokenAmount.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import type { Token } from '~/types/generated'
import { TokenMetadata } from '~/types/tokens'

type Props = {
	token: Token
	metadata?: TokenMetadata
}

defineProps<Props>()
</script>
