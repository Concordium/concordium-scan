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

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Account creation
					<template #content>
						<span class="numerical">
							{{ event.payload.accountCreation }}
						</span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Baker
					<template #content>
						<span class="numerical">
							{{ event.payload.baker }}
						</span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Chain update
					<template #content>
						<span class="numerical">
							{{ event.payload.chainUpdate }}
						</span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Finalisation proof
					<template #content>
						<span class="numerical">
							{{ event.payload.finalizationProof }}
						</span>
					</template>
				</DescriptionListItem>
			</DescriptionList>
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

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Baking reward account
					<template #content>
						<span class="numerical"> {{ event.payload.bakingReward }}% </span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Finalisation reward account
					<template #content>
						<span class="numerical">
							{{ event.payload.finalizationReward }}%
						</span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Mint per slot
					<template #content>
						<span class="numerical">
							{{ event.payload.mintPerSlot }}
						</span>
					</template>
				</DescriptionListItem>
			</DescriptionList>
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

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Baker account
					<template #content>
						<span class="numerical"> {{ event.payload.baker }}% </span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Gas account
					<template #content>
						<span class="numerical"> {{ event.payload.gasAccount }}% </span>
					</template>
				</DescriptionListItem>
			</DescriptionList>
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
import DescriptionList from '~/components/atoms/DescriptionList.vue'
import DescriptionListItem from '~/components/atoms/DescriptionListItem.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import ExternalLink from '~/components/molecules/ExternalLink.vue'
import type { ChainUpdateEnqueued } from '~/types/generated'

const { NOW } = useDateNow()

type Props = {
	event: ChainUpdateEnqueued
}

defineProps<Props>()
</script>
