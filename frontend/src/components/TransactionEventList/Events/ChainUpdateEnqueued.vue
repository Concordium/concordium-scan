<template>
	<span>
		Chain update enqueued to be effective at
		{{ formatTimestamp(event.effectiveTime) }}
		({{ convertTimestampToRelative(event.effectiveTime, NOW, true) }})

		<span
			v-if="
				event.payload.__typename === 'AddAnonymityRevokerChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Add anonymity revoker '{{ event.payload.name }}'
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'AddIdentityProviderChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Add identity provider '{{ event.payload.name }}'
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'BakerStakeThresholdChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Update baker stake threshold to
			{{ convertMicroCcdToCcd(event.payload.amount) }}Ͼ
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'ElectionDifficultyChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Update election difficulty to {{ event.payload.electionDifficulty }}%
		</span>

		<span
			v-else-if="event.payload.__typename === 'EuroPerEnergyChainUpdatePayload'"
			class="text-theme-faded"
		>
			<br />
			Update EUR/ENERGY exchange rate to
			{{
				event.payload.exchangeRate.numerator /
				event.payload.exchangeRate.denominator
			}}
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'FoundationAccountChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Change foundation account to
			<AccountLink :address="event.payload.accountAddress.asString" />
		</span>

		<span
			v-else-if="event.payload.__typename === 'GasRewardsChainUpdatePayload'"
			class="text-theme-faded"
		>
			<br />
			Update gas rewards to:

			<dl class="flex flex-wrap justify-between pl-4 pt-2">
				<dt class="w-1/2">Account creation</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.accountCreation }}
				</dd>
				<dt class="w-1/2">Baker</dt>
				<dd class="w-1/2 numerical text-right">{{ event.payload.baker }}</dd>
				<dt class="w-1/2">Chain update</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.chainUpdate }}
				</dd>
				<dt class="w-1/2">Finalisation proof</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.finalizationProof }}
				</dd>
			</dl>
		</span>

		<span
			v-else-if="event.payload.__typename === 'Level1KeysChainUpdatePayload'"
			class="text-theme-faded"
		>
			<br />
			Update Level 1 keys
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'MicroCcdPerEuroChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Update CCD/EUR exchange rate to
			{{
				convertMicroCcdToCcd(
					event.payload.exchangeRate.numerator /
						event.payload.exchangeRate.denominator
				)
			}}

			(1Ͼ ≈
			{{
				formatNumber(
					(event.payload.exchangeRate.denominator /
						event.payload.exchangeRate.numerator) *
						1_000_000
				)
			}}€)
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'MintDistributionChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Update mint distribution to:

			<dl class="flex flex-wrap justify-between pl-4 pt-2">
				<dt class="w-1/2">Baking reward account</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.bakingReward }}%
				</dd>
				<dt class="w-1/2">Finalisation reward account</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.finalizationReward }}%
				</dd>
				<dt class="w-1/2">Mint per slot</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.mintPerSlot }}
				</dd>
			</dl>
		</span>

		<span
			v-else-if="event.payload.__typename === 'ProtocolChainUpdatePayload'"
			class="text-theme-faded"
		>
			<br />
			Update protocol: '{{ event.payload.message }}'.
			<ExternalLink :href="event.payload.specificationUrl"
				>See specification</ExternalLink
			>
		</span>

		<span
			v-else-if="event.payload.__typename === 'RootKeysChainUpdatePayload'"
			class="text-theme-faded"
		>
			<br />
			Update root keys
		</span>

		<span
			v-else-if="
				event.payload.__typename ===
				'TransactionFeeDistributionChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			<br />
			Update transaction fee distribution to:
			<dl class="flex flex-wrap justify-between pl-4 pt-2">
				<dt class="w-1/2">Baker account</dt>
				<dd class="w-1/2 numerical text-right">{{ event.payload.baker }}%</dd>
				<dt class="w-1/2">Gas account</dt>
				<dd class="w-1/2 numerical text-right">
					{{ event.payload.gasAccount }}%
				</dd>
			</dl>
		</span>
	</span>
</template>

<script setup lang="ts">
import {
	formatNumber,
	formatTimestamp,
	convertMicroCcdToCcd,
	convertTimestampToRelative,
} from '~/utils/format'
import { useDateNow } from '~/composables/useDateNow'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ExternalLink from '~/components/molecules/ExternalLink.vue'
import type { ChainUpdateEnqueued } from '~/types/generated'

const { NOW } = useDateNow()

type Props = {
	event: ChainUpdateEnqueued
}

defineProps<Props>()
</script>
