<template>
	<dl class="grid grid-cols-2 col-span-2 px-4">
		<dt v-if="delegation.pendingChange" id="delegatoraccordion-pendingchange">
			Pending change
		</dt>
		<dd
			v-if="delegation.pendingChange"
			class="text-right mb-2"
			aria-labelledby="delegatoraccordion-pendingchange"
		>
			<div
				v-if="
					delegation.pendingChange?.__typename === 'PendingDelegationRemoval'
				"
			>
				Removal at
				<Tooltip
					:text="formatTimestamp(delegation.pendingChange?.effectiveTime)"
				>
					{{
						convertTimestampToRelative(
							delegation.pendingChange?.effectiveTime,
							NOW
						)
					}}
				</Tooltip>
			</div>
			<div
				v-else-if="
					delegation.pendingChange?.__typename ===
					'PendingDelegationReduceStake'
				"
			>
				Reducing stake to
				<Amount :amount="delegation.pendingChange?.newStakedAmount" />
				at
				<Tooltip
					:text="formatTimestamp(delegation.pendingChange?.effectiveTime)"
				>
					{{
						convertTimestampToRelative(
							delegation.pendingChange?.effectiveTime,
							NOW
						)
					}}
				</Tooltip>
			</div>
			<div v-else>
				Unknown pending change {{ delegation.pendingChange?.__typename }}
			</div>
		</dd>
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
				Passive delegation
			</span>
		</dd>
	</dl>
</template>

<script lang="ts" setup>
import { formatTimestamp, convertTimestampToRelative } from '~/utils/format'
import type { Delegation } from '~/types/generated'
import BakerLink from '~/components/molecules/BakerLink.vue'
import Tooltip from '~/components/atoms/Tooltip.vue'
import Amount from '~/components/atoms/Amount.vue'
const { NOW } = useDateNow()

type Props = {
	delegation: Delegation
}

defineProps<Props>()
</script>
