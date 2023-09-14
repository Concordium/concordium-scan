<template>
	<div>
		<ul class="px-4">
			<li
				v-for="(event, i) in events?.nodes"
				:key="i"
				class="border-l-4 py-4 px-6 relative"
				:class="$style.listItem"
			>
				<AccountCreated
					v-if="event.__typename === 'AccountCreated'"
					:event="event"
				/>

				<AmountAddedByDecryption
					v-else-if="event.__typename === 'AmountAddedByDecryption'"
					:event="event"
				/>

				<BakerAdded
					v-else-if="event.__typename === 'BakerAdded'"
					:event="event"
				/>

				<BakerKeysUpdated
					v-else-if="event.__typename === 'BakerKeysUpdated'"
					:event="event"
				/>

				<BakerRemoved
					v-else-if="event.__typename === 'BakerRemoved'"
					:event="event"
				/>

				<BakerSetRestakeEarnings
					v-else-if="event.__typename === 'BakerSetRestakeEarnings'"
					:event="event"
				/>

				<BakerStakeDecreased
					v-else-if="event.__typename === 'BakerStakeDecreased'"
					:event="event"
				/>

				<BakerStakeIncreased
					v-else-if="event.__typename === 'BakerStakeIncreased'"
					:event="event"
				/>

				<ContractInitialized
					v-else-if="event.__typename === 'ContractInitialized'"
					:event="event"
				/>

				<ContractModuleDeployed
					v-else-if="event.__typename === 'ContractModuleDeployed'"
					:event="event"
				/>

				<ContractUpdated
					v-else-if="event.__typename === 'ContractUpdated'"
					:event="event"
				/>

				<ContractInterrupted
					v-else-if="event.__typename === 'ContractInterrupted'"
					:event="event"
				/>

				<ContractResumed
					v-else-if="event.__typename === 'ContractResumed'"
					:event="event"
				/>

				<CredentialDeployed
					v-else-if="event.__typename === 'CredentialDeployed'"
					:event="event"
				/>

				<CredentialKeysUpdated
					v-else-if="event.__typename === 'CredentialKeysUpdated'"
					:event="event"
				/>

				<CredentialsUpdated
					v-else-if="event.__typename === 'CredentialsUpdated'"
					:event="event"
				/>
				<ContractUpgraded
					v-else-if="event.__typename === 'ContractUpgraded'"
					:event="event"
				/>
				<DataRegistered
					v-else-if="event.__typename === 'DataRegistered'"
					:event="event"
				/>

				<EncryptedAmountsRemoved
					v-else-if="event.__typename === 'EncryptedAmountsRemoved'"
					:event="event"
				/>

				<EncryptedSelfAmountAdded
					v-else-if="event.__typename === 'EncryptedSelfAmountAdded'"
					:event="event"
				/>

				<NewEncryptedAmount
					v-else-if="event.__typename === 'NewEncryptedAmount'"
					:event="event"
				/>

				<TransferMemo
					v-else-if="event.__typename === 'TransferMemo'"
					:event="event"
				/>

				<Transferred
					v-else-if="event.__typename === 'Transferred'"
					:event="event"
				/>

				<TransferredWithSchedule
					v-else-if="event.__typename === 'TransferredWithSchedule'"
					:event="event"
					:transaction="props.transaction"
				/>

				<ChainUpdateEnqueued
					v-else-if="event.__typename === 'ChainUpdateEnqueued'"
					:event="event"
				/>
				<BakerSetOpenStatus
					v-else-if="event.__typename === 'BakerSetOpenStatus'"
					:event="event"
				/>
				<BakerSetMetadataURL
					v-else-if="event.__typename === 'BakerSetMetadataURL'"
					:event="event"
				/>
				<BakerSetTransactionFeeCommission
					v-else-if="event.__typename === 'BakerSetTransactionFeeCommission'"
					:event="event"
				/>
				<BakerSetBakingRewardCommission
					v-else-if="event.__typename === 'BakerSetBakingRewardCommission'"
					:event="event"
				/>
				<BakerSetFinalizationRewardCommission
					v-else-if="
						event.__typename === 'BakerSetFinalizationRewardCommission'
					"
					:event="event"
				/>
				<DelegationStakeIncreased
					v-else-if="event.__typename === 'DelegationStakeIncreased'"
					:event="event"
				/>
				<DelegationStakeDecreased
					v-else-if="event.__typename === 'DelegationStakeDecreased'"
					:event="event"
				/>
				<DelegationSetRestakeEarnings
					v-else-if="event.__typename === 'DelegationSetRestakeEarnings'"
					:event="event"
				/>
				<DelegationSetDelegationTarget
					v-else-if="event.__typename === 'DelegationSetDelegationTarget'"
					:event="event"
				/>
				<DelegationAdded
					v-else-if="event.__typename === 'DelegationAdded'"
					:event="event"
				/>
				<DelegationRemoved
					v-else-if="event.__typename === 'DelegationRemoved'"
					:event="event"
				/>
				<span v-else>Transaction event: {{ event.__typename }}</span>
			</li>
		</ul>
		<Pagination
			v-if="events?.pageInfo && events?.totalCount > PAGE_SIZE"
			:page-info="events?.pageInfo"
			:go-to-page="props.goToPage"
		/>
	</div>
</template>

<script lang="ts" setup>
import AccountCreated from './Events/AccountCreated.vue'
import AmountAddedByDecryption from './Events/AmountAddedByDecryption.vue'
import BakerAdded from './Events/BakerAdded.vue'
import BakerKeysUpdated from './Events/BakerKeysUpdated.vue'
import BakerRemoved from './Events/BakerRemoved.vue'
import BakerSetRestakeEarnings from './Events/BakerSetRestakeEarnings.vue'
import BakerStakeDecreased from './Events/BakerStakeDecreased.vue'
import BakerStakeIncreased from './Events/BakerStakeIncreased.vue'
import ChainUpdateEnqueued from './Events/ChainUpdateEnqueued.vue'
import ContractInitialized from './Events/ContractInitialized.vue'
import ContractModuleDeployed from './Events/ContractModuleDeployed.vue'
import ContractInterrupted from './Events/ContractInterrupted.vue'
import ContractResumed from './Events/ContractResumed.vue'
import ContractUpdated from './Events/ContractUpdated.vue'
import ContractUpgraded from './Events/ContractUpgraded.vue'
import CredentialDeployed from './Events/CredentialDeployed.vue'
import CredentialKeysUpdated from './Events/CredentialKeysUpdated.vue'
import CredentialsUpdated from './Events/CredentialsUpdated.vue'
import DataRegistered from './Events/DataRegistered.vue'
import EncryptedAmountsRemoved from './Events/EncryptedAmountsRemoved.vue'
import EncryptedSelfAmountAdded from './Events/EncryptedSelfAmountAdded.vue'
import NewEncryptedAmount from './Events/NewEncryptedAmount.vue'
import TransferMemo from './Events/TransferMemo.vue'
import Transferred from './Events/Transferred.vue'
import TransferredWithSchedule from './Events/TransferredWithSchedule.vue'
import BakerSetOpenStatus from '~/components/TransactionEventList/Events/BakerSetOpenStatus.vue'
import BakerSetMetadataURL from '~/components/TransactionEventList/Events/BakerSetMetadataURL.vue'
import BakerSetTransactionFeeCommission from '~/components/TransactionEventList/Events/BakerSetTransactionFeeCommission.vue'
import BakerSetBakingRewardCommission from '~/components/TransactionEventList/Events/BakerSetBakingRewardCommission.vue'
import BakerSetFinalizationRewardCommission from '~/components/TransactionEventList/Events/BakerSetFinalizationRewardCommission.vue'
import DelegationStakeIncreased from '~/components/TransactionEventList/Events/DelegationStakeIncreased.vue'
import DelegationStakeDecreased from '~/components/TransactionEventList/Events/DelegationStakeDecreased.vue'
import DelegationSetRestakeEarnings from '~/components/TransactionEventList/Events/DelegationSetRestakeEarnings.vue'
import DelegationSetDelegationTarget from '~/components/TransactionEventList/Events/DelegationSetDelegationTarget.vue'
import DelegationAdded from '~/components/TransactionEventList/Events/DelegationAdded.vue'
import DelegationRemoved from '~/components/TransactionEventList/Events/DelegationRemoved.vue'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Success, PageInfo, Transaction } from '~/types/generated'

type Props = {
	events: Success['events']
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
	transaction: Transaction
}

const props = defineProps<Props>()
</script>

<style module>
.listItem {
	border-color: hsl(var(--color-primary));
}

/* 1: Half of it's own width, half of the border width */
.listItem::before {
	content: '';
	display: block;
	background: hsl(var(--color-primary));
	height: 1rem;
	width: 1rem;
	position: absolute;
	top: 1rem;
	left: calc(-0.5rem - 2px); /* 1 */
	border-radius: 50%;
}
</style>
