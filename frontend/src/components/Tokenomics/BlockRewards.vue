<template>
	<TokenomicsDisplay class="p-4">
		<template #title>Baker rewards</template>
		<template #content>
			<dl class="grid grid-cols-2 col-span-2">
				<dt>Baker</dt>
				<dd class="text-right mb-2">
					<AccountLink :address="data.bakerAccountAddressString" />
				</dd>
				<dt>Baker reward</dt>
				<dd class="text-right mb-2">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.bakerReward) }}
					</span>
					Ͼ
				</dd>
				<dt>Transaction fees</dt>
				<dd class="text-right mb-2">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.transactionFees) }}
					</span>
					Ͼ
				</dd>
				<dt>Foundation charge</dt>
				<dd class="text-right mb-2">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.foundationCharge) }}
					</span>
					Ͼ
				</dd>
				<dt>Gas account</dt>
				<dd class="text-right mb-2">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.oldGasAccount) }}
					</span>
					Ͼ
				</dd>
				<dt>New gas account</dt>
				<dd class="text-right mb-2">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.newGasAccount) }}
					</span>
					Ͼ
				</dd>
			</dl>
		</template>
	</TokenomicsDisplay>
</template>

<script lang="ts" setup>
import TokenomicsDisplay from './TokenomicsDisplay.vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import type { BlockRewards } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'

type Props = {
	data: BlockRewards
}

defineProps<Props>()
</script>

<style>
.totalRow {
	border-top: solid 1px white;
	margin-top: 4px;
	padding-top: 4px;
}
</style>
