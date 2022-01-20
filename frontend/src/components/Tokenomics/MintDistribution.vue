<template>
	<TokenomicsDisplay>
		<template #title>Distributed minted CCD</template>
		<template #content>
			<dl class="grid grid-cols-2 col-span-2">
				<dt>Baking reward account</dt>
				<dd class="text-right">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.bakingReward) }}
					</span>
					Ͼ
				</dd>
				<dt>Finalisation reward account</dt>
				<dd class="text-right">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.finalizationReward) }}
					</span>
					Ͼ
				</dd>
				<dt>Foundation account</dt>
				<dd class="text-right">
					<span class="numerical">
						{{ convertMicroCcdToCcd(data.platformDevelopmentCharge) }}
					</span>
					Ͼ
				</dd>
				<dt class="totalRow">= TOTAL MINTED</dt>
				<dd class="totalRow text-right">
					<span class="numerical">
						{{
							convertMicroCcdToCcd(
								data.platformDevelopmentCharge +
									data.finalizationReward +
									data.bakingReward
							)
						}}
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
import type { Mint } from '~/types/blocks'

type Props = {
	data: Mint
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
