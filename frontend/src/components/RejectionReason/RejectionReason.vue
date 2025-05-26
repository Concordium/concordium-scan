'
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
		<MissingBakerAddParameters
			v-else-if="reason.__typename === 'MissingBakerAddParameters'"
			:reason="reason"
		/>
		<FinalizationRewardCommissionNotInRange
			v-else-if="reason.__typename === 'FinalizationRewardCommissionNotInRange'"
			:reason="reason"
		/>
		<BakingRewardCommissionNotInRange
			v-else-if="reason.__typename === 'BakingRewardCommissionNotInRange'"
			:reason="reason"
		/>
		<TransactionFeeCommissionNotInRange
			v-else-if="reason.__typename === 'TransactionFeeCommissionNotInRange'"
			:reason="reason"
		/>
		<AlreadyADelegator
			v-else-if="reason.__typename === 'AlreadyADelegator'"
			:reason="reason"
		/>
		<InsufficientBalanceForDelegationStake
			v-else-if="reason.__typename === 'InsufficientBalanceForDelegationStake'"
			:reason="reason"
		/>
		<MissingDelegationAddParameters
			v-else-if="reason.__typename === 'MissingDelegationAddParameters'"
			:reason="reason"
		/>
		<InsufficientDelegationStake
			v-else-if="reason.__typename === 'InsufficientDelegationStake'"
			:reason="reason"
		/>
		<DelegatorInCooldown
			v-else-if="reason.__typename === 'DelegatorInCooldown'"
			:reason="reason"
		/>
		<NotADelegator
			v-else-if="reason.__typename === 'NotADelegator'"
			:reason="reason"
		/>
		<DelegationTargetNotABaker
			v-else-if="reason.__typename === 'DelegationTargetNotABaker'"
			:reason="reason"
		/>
		<StakeOverMaximumThresholdForPool
			v-else-if="reason.__typename === 'StakeOverMaximumThresholdForPool'"
			:reason="reason"
		/>
		<PoolWouldBecomeOverDelegated
			v-else-if="reason.__typename === 'PoolWouldBecomeOverDelegated'"
			:reason="reason"
		/>
		<PoolClosed
			v-else-if="reason.__typename === 'PoolClosed'"
			:reason="reason"
		/>
		<NonExistentTokenId
			v-else-if="reason.__typename === 'NonExistentTokenId'"
			:reason="reason"
		/>
		<TokenModule
			v-else-if="reason.__typename === 'TokenModule'"
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
import NonExistentTokenId from './Reasons/NonExistentTokenId.vue'
import TokenModule from './Reasons/TokenModule.vue'
import { translateRejectionReasons } from '~/utils/translateRejectionReasons'
import type { TransactionRejectReason } from '~/types/generated'
import MissingBakerAddParameters from '~/components/RejectionReason/Reasons/MissingBakerAddParameters.vue'
import FinalizationRewardCommissionNotInRange from '~/components/RejectionReason/Reasons/FinalizationRewardCommissionNotInRange.vue'
import BakingRewardCommissionNotInRange from '~/components/RejectionReason/Reasons/BakingRewardCommissionNotInRange.vue'
import TransactionFeeCommissionNotInRange from '~/components/RejectionReason/Reasons/TransactionFeeCommissionNotInRange.vue'
import AlreadyADelegator from '~/components/RejectionReason/Reasons/AlreadyADelegator.vue'
import InsufficientBalanceForDelegationStake from '~/components/RejectionReason/Reasons/InsufficientBalanceForDelegationStake.vue'
import MissingDelegationAddParameters from '~/components/RejectionReason/Reasons/MissingDelegationAddParameters.vue'
import InsufficientDelegationStake from '~/components/RejectionReason/Reasons/InsufficientDelegationStake.vue'
import DelegatorInCooldown from '~/components/RejectionReason/Reasons/DelegatorInCooldown.vue'
import NotADelegator from '~/components/RejectionReason/Reasons/NotADelegator.vue'
import DelegationTargetNotABaker from '~/components/RejectionReason/Reasons/DelegationTargetNotABaker.vue'
import StakeOverMaximumThresholdForPool from '~/components/RejectionReason/Reasons/StakeOverMaximumThresholdForPool.vue'
import PoolWouldBecomeOverDelegated from '~/components/RejectionReason/Reasons/PoolWouldBecomeOverDelegated.vue'
import PoolClosed from '~/components/RejectionReason/Reasons/PoolClosed.vue'

type Props = {
	reason: TransactionRejectReason
}

defineProps<Props>()
</script>
