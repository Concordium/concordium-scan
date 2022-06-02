<template>
	<span>
		Delegator <AccountLink :address="event.accountAddress.asString" /> (id
		{{ event.delegatorId }}) set delegation target to
		<BakerLink
			v-if="event.delegationTarget.__typename === 'BakerDelegationTarget'"
			:id="event.delegationTarget.bakerId"
		/>
		<span
			v-else-if="
				event.delegationTarget.__typename === 'PassiveDelegationTarget'
			"
		>
			<PassiveDelegationLink />
		</span>
	</span>
</template>

<script setup lang="ts">
import type { DelegationSetDelegationTarget } from '~/types/generated'
import AccountLink from '~/components/molecules/AccountLink.vue'
import BakerLink from '~/components/molecules/BakerLink.vue'
import PassiveDelegationLink from '~/components/molecules/PassiveDelegationLink.vue'

type Props = {
	event: DelegationSetDelegationTarget
}

defineProps<Props>()
</script>
