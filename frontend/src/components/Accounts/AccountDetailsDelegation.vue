<template>
	<dl class="grid grid-cols-2 col-span-2 px-4">
		<dt id="delegatoraccordion-bakerid">Delegator ID</dt>
		<dd class="text-right mb-2" aria-labelledby="bakeraccordion-bakerid">
			{{ delegation.delegatorId }}
		</dd>
		<dt id="delegatoraccordion-stakedamount">Staked amount</dt>
		<dd
			class="text-right mb-2"
			aria-labelledby="delegatoraccordion-stakedamount"
		>
			<span class="numerical">
				<Amount :amount="delegation.stakedAmount" :show-symbol="true" />
			</span>
		</dd>
		<dt id="delegatoraccordion-restakeearnings">Restake earnings?</dt>
		<dd
			class="text-right mb-2"
			aria-labelledby="delegatoraccordion-restakeearnings"
		>
			<div v-if="delegation.restakeEarnings">Yes</div>
			<div v-else>No</div>
		</dd>
		<dt id="delegatoraccordion-delegationtarget">Target</dt>
		<dd
			class="text-right mb-2"
			aria-labelledby="delegatoraccordion-delegationtarget"
		>
			<BakerLink
				v-if="
					delegation.delegationTarget.__typename === 'BakerDelegationTarget'
				"
				:id="delegation.delegationTarget.bakerId"
			/>
			<span
				v-else-if="
					delegation.delegationTarget.__typename === 'PassiveDelegationTarget'
				"
			>
				<PassiveDelegationLink />
			</span>
		</dd>
	</dl>
</template>

<script lang="ts" setup>
import type { Delegation } from '~/types/generated'
import BakerLink from '~/components/molecules/BakerLink.vue'
import Amount from '~/components/atoms/Amount.vue'
import PassiveDelegationLink from '~/components/molecules/PassiveDelegationLink.vue'

type Props = {
	delegation: Delegation
}

defineProps<Props>()
</script>
