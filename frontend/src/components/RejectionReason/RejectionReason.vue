<template>
	<div class="px-4">
		<ModuleHashAlreadyExists
			v-if="reason.__typename === 'ModuleHashAlreadyExists'"
			:reason="reason"
		/>

		<InvalidAccountReference
			v-else-if="reason.__typename === 'InvalidAccountReference'"
			:reason="reason"
		/>

		<InvalidInitMethod
			v-else-if="reason.__typename === 'InvalidInitMethod'"
			:reason="reason"
		/>

		<InvalidReceiveMethod
			v-else-if="reason.__typename === 'InvalidReceiveMethod'"
			:reason="reason"
		/>

		<InvalidModuleReference
			v-else-if="reason.__typename === 'InvalidModuleReference'"
			:reason="reason"
		/>

		<InvalidContractAddress
			v-else-if="reason.__typename === 'InvalidContractAddress'"
			:reason="reason"
		/>

		<AmountTooLarge
			v-else-if="reason.__typename === 'AmountTooLarge'"
			:reason="reason"
		/>

		<RejectedInit
			v-else-if="reason.__typename === 'RejectedInit'"
			:reason="reason"
		/>

		<RejectedReceive
			v-else-if="reason.__typename === 'RejectedReceive'"
			:reason="reason"
		/>

		<NonExistentRewardAccount
			v-else-if="reason.__typename === 'NonExistentRewardAccount'"
			:reason="reason"
		/>

		<AlreadyABaker
			v-else-if="reason.__typename === 'AlreadyABaker'"
			:reason="reason"
		/>

		<NotABaker v-else-if="reason.__typename === 'NotABaker'" :reason="reason" />

		<EncryptedAmountSelfTransfer
			v-else-if="reason.__typename === 'EncryptedAmountSelfTransfer'"
			:reason="reason"
		/>

		<ScheduledSelfTransfer
			v-else-if="reason.__typename === 'ScheduledSelfTransfer'"
			:reason="reason"
		/>

		<DuplicateCredIds
			v-else-if="reason.__typename === 'DuplicateCredIds'"
			:reason="reason"
		/>

		<NonExistentCredIds
			v-else-if="reason.__typename === 'NonExistentCredIds'"
			:reason="reason"
		/>

		<span v-else>{{ translateRejectionReasons(reason) }}</span>
	</div>
</template>

<script lang="ts" setup>
import ModuleHashAlreadyExists from './Reasons/ModuleHashAlreadyExists.vue'
import InvalidAccountReference from './Reasons/InvalidAccountReference.vue'
import InvalidInitMethod from './Reasons/InvalidInitMethod.vue'
import InvalidReceiveMethod from './Reasons/InvalidReceiveMethod.vue'
import InvalidModuleReference from './Reasons/InvalidModuleReference.vue'
import InvalidContractAddress from './Reasons/InvalidContractAddress.vue'
import AmountTooLarge from './Reasons/AmountTooLarge.vue'
import RejectedInit from './Reasons/RejectedInit.vue'
import RejectedReceive from './Reasons/RejectedReceive.vue'
import NonExistentRewardAccount from './Reasons/NonExistentRewardAccount.vue'
import AlreadyABaker from './Reasons/AlreadyABaker.vue'
import NotABaker from './Reasons/NotABaker.vue'
import EncryptedAmountSelfTransfer from './Reasons/EncryptedAmountSelfTransfer.vue'
import ScheduledSelfTransfer from './Reasons/ScheduledSelfTransfer.vue'
import DuplicateCredIds from './Reasons/DuplicateCredIds.vue'
import NonExistentCredIds from './Reasons/NonExistentCredIds.vue'
import { translateRejectionReasons } from '~/utils/translateRejectionReasons'
import type { TransactionRejectReason } from '~/types/generated'

type Props = {
	reason: TransactionRejectReason
}

defineProps<Props>()
</script>
