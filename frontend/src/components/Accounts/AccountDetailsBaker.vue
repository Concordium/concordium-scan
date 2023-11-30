<template>
	<dl class="grid grid-cols-2 col-span-2 px-4">
		<dt id="bakeraccordion-bakerid">Validator ID</dt>
		<dd class="text-right mb-2" aria-labelledby="bakeraccordion-bakerid">
			<BakerLink :id="baker.bakerId" />
		</dd>
		<dt
			v-if="baker.state.__typename === 'ActiveBakerState'"
			id="bakeraccordion-stakedamount"
		>
			Staked amount
		</dt>
		<dd
			v-if="baker.state.__typename === 'ActiveBakerState'"
			class="text-right mb-2"
			aria-labelledby="bakeraccordion-stakedamount"
		>
			<Amount :show-symbol="true" :amount="baker.state.stakedAmount" />
		</dd>
		<dt
			v-if="baker.state.__typename === 'RemovedBakerState'"
			id="bakeraccordion-removedat"
		>
			Removed at
		</dt>
		<dd
			v-if="baker.state.__typename === 'RemovedBakerState'"
			class="text-right mb-2"
			aria-labelledby="bakeraccordion-removedat"
		>
			The validator was removed at
			{{ formatTimestamp(baker.state.removedAt) }}
		</dd>
	</dl>
</template>

<script lang="ts" setup>
import { formatTimestamp } from '~/utils/format'
import type { Baker } from '~/types/generated'
import BakerLink from '~/components/molecules/BakerLink.vue'
import Amount from '~/components/atoms/Amount.vue'

type Props = {
	baker: Baker
}

defineProps<Props>()
</script>
