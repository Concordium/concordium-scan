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
						<TransactionLink :hash="contractEvent.transactionHash" />
					</TableTd>
					<TableTd>
						<ContractInitialized
							v-if="contractEvent.event.__typename === 'ContractInitialized'"
							:event="contractEvent.event"
						/>
						<ContractModuleDeployed
							v-else-if="
								contractEvent.event.__typename === 'ContractModuleDeployed'
							"
							:event="contractEvent.event"
						/>
						<ContractUpdated
							v-else-if="contractEvent.event.__typename === 'ContractUpdated'"
							:event="contractEvent.event"
						/>
						<ContractUpgraded
							v-else-if="contractEvent.event.__typename === 'ContractUpgraded'"
							:event="contractEvent.event"
						/>
						<ContractInterrupted
							v-else-if="
								contractEvent.event.__typename === 'ContractInterrupted'
							"
							:event="contractEvent.event"
						/>
						<ContractResumed
							v-else-if="contractEvent.event.__typename === 'ContractResumed'"
							:event="contractEvent.event"
						/>
						<TransferMemo
							v-else-if="contractEvent.event.__typename === 'TransferMemo'"
							:event="contractEvent.event"
						/>
						<Transferred
							v-else-if="contractEvent.event.__typename === 'Transferred'"
							:event="contractEvent.event"
						/>
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</div>
</template>

<script lang="ts" setup>
import ContractInitialized from '~/components/TransactionEventList/Events/ContractInitialized.vue'
import ContractModuleDeployed from '~/components/TransactionEventList/Events/ContractModuleDeployed.vue'
import ContractInterrupted from '~/components/TransactionEventList/Events/ContractInterrupted.vue'
import ContractResumed from '~/components/TransactionEventList/Events/ContractResumed.vue'
import ContractUpdated from '~/components/TransactionEventList/Events/ContractUpdated.vue'
import ContractUpgraded from '~/components/TransactionEventList/Events/ContractUpgraded.vue'
import TransferMemo from '~/components/TransactionEventList/Events/TransferMemo.vue'
import Transferred from '~/components/TransactionEventList/Events/Transferred.vue'
import { ContractEvent } from '~~/src/types/generated'
import TransactionLink from '~~/src/components/molecules/TransactionLink.vue'

type Props = {
	contractEvents: ContractEvent[]
}
defineProps<Props>()
</script>
