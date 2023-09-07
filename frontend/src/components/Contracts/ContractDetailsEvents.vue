<template>
	<div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Transaction Hash</TableTh>
					<TableTh>Type</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="contractEvent in contractEvents" :key="contractEvent">
					<TableTd class="numerical">
						<TransactionResult :result="contractEvent.event.__typename" />
						<TransactionLink :hash="contractEvent.transactionHash" />
					</TableTd>
					<TableTd>
						<div class="whitespace-normal">
							{{ contractEvent.event.__typename }}
						</div>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</div>
</template>

<script lang="ts" setup>
import { ContractEvent } from '~~/src/types/generated'

type Props = {
	contractEvents: ContractEvent[]
}
defineProps<Props>()
</script>
