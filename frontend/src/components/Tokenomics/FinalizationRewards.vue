<template>
	<TokenomicsDisplay>
		<template #title>Finalisers</template>
		<template #content>
			<Table>
				<TableHead>
					<TableRow>
						<TableTh>Finaliser</TableTh>
						<TableTh>Weight</TableTh>
						<TableTh align="right">Reward (Ͼ)</TableTh>
					</TableRow>
				</TableHead>
				<TableBody>
					<TableRow v-for="finalizer in data" :key="finalizer.address">
						<TableTd>
							<UserIcon class="text-white inline h-4 align-baseline" />
							{{ finalizer.address.substring(0, 6) }}
						</TableTd>
						<TableTd class="numerical text-right">
							{{ calculateWeight(finalizer.amount, totalAmount) }}%
						</TableTd>
						<TableTd align="right" class="numerical">
							{{ convertMicroCcdToCcd(finalizer.amount) }}
						</TableTd>
					</TableRow>
				</TableBody>
			</Table>
		</template>
	</TokenomicsDisplay>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import TokenomicsDisplay from './TokenomicsDisplay.vue'
import { convertMicroCcdToCcd, calculateWeight } from '~/utils/format'
import type { FinalizationReward } from '~/types/blocks'

type Props = {
	data: FinalizationReward[]
}

const props = defineProps<Props>()

const totalAmount = props.data.reduce((sum, curr) => sum + curr.amount, 0)
</script>
