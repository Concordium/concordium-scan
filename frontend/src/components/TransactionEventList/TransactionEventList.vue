<template>
	<div>
		<ul class="px-4">
			<li
				v-for="(event, i) in events?.nodes"
				:key="i"
				class="border-l-4 py-4 px-6 relative"
				:class="$style.listItem"
			>
				<AmountAddedByDecryption
					v-if="event.__typename === 'AmountAddedByDecryption'"
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
				/>

				<ChainUpdateEnqueued
					v-else-if="event.__typename === 'ChainUpdateEnqueued'"
					:event="event"
				/>

				<span v-else>{{ translateTransactionEvents(event) }}</span>
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
import ContractUpdated from './Events/ContractUpdated.vue'
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
import { translateTransactionEvents } from '~/utils/translateTransactionEvents'
import { PAGE_SIZE } from '~/composables/usePagination'
import type { PaginationTarget } from '~/composables/usePagination'
import type { Success, PageInfo } from '~/types/generated'

type Props = {
	events: Success['events']
	goToPage: (page: PageInfo) => (target: PaginationTarget) => void
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
