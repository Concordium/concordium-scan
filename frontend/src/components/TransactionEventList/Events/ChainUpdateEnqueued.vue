<template>
	<span>
		Chain update enqueued to be effective at
		{{ formatTimestamp(event.effectiveTime) }}
		({{ convertTimestampToRelative(event.effectiveTime, NOW, true) }})
		<br />

		<span
			v-if="
				event.payload.__typename === 'AddAnonymityRevokerChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Add anonymity revoker '{{ event.payload.name }}'
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'AddIdentityProviderChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Add identity provider '{{ event.payload.name }}'
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'BakerStakeThresholdChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Update baker stake threshold to
			{{ convertMicroCcdToCcd(event.payload.amount) }}Ͼ
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'ElectionDifficultyChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Update election difficulty to {{ event.payload.electionDifficulty }}%
		</span>

		<span
			v-else-if="event.payload.__typename === 'EuroPerEnergyChainUpdatePayload'"
			class="text-theme-faded"
		>
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
			Change foundation account to
			<AccountLink :address="event.payload.accountAddress.asString" />
		</span>

		<span
			v-else-if="event.payload.__typename === 'GasRewardsChainUpdatePayload'"
			class="text-theme-faded"
		>
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
					Finalization proof
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
			Update Level 1 keys
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'MicroCcdPerEuroChainUpdatePayload'
			"
			class="text-theme-faded"
		>
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
			Update mint distribution to:

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Baking reward account
					<template #content>
						<span class="numerical"> {{ event.payload.bakingReward }}% </span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Finalization reward account
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
			Update protocol: '{{ event.payload.message }}'.
			<ExternalLink :href="event.payload.specificationUrl"
				>See specification</ExternalLink
			>
		</span>

		<span
			v-else-if="event.payload.__typename === 'RootKeysChainUpdatePayload'"
			class="text-theme-faded"
		>
			Update root keys
		</span>

		<span
			v-else-if="
				event.payload.__typename ===
				'TransactionFeeDistributionChainUpdatePayload'
			"
			class="text-theme-faded"
		>
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

		<span
			v-else-if="
				event.payload.__typename === 'CooldownParametersChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Update cooldown parameters to:

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Delegator cooldown
					<template #content>
						<span class="numerical">
							{{ formatNumber(event.payload.delegatorCooldown) }}s
						</span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Pool owner cooldown
					<template #content>
						<span class="numerical">
							{{ formatNumber(event.payload.poolOwnerCooldown) }}s
						</span>
					</template>
				</DescriptionListItem>
			</DescriptionList>
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'PoolParametersChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Update pool parameters to:

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Baking commission range
					<template #content>
						<span class="numerical">
							{{ event.payload.bakingCommissionRange.min * 100 }} </span
						>% -
						<span class="numerical">
							{{ event.payload.bakingCommissionRange.max * 100 }} </span
						>%
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Finalization commission range
					<template #content>
						<span class="numerical">
							{{ event.payload.finalizationCommissionRange.min * 100 }}</span
						>% -
						<span class="numerical">
							{{ event.payload.finalizationCommissionRange.max * 100 }} </span
						>%
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Transaction commission range
					<template #content>
						<span class="numerical">
							{{ event.payload.transactionCommissionRange.min * 100 }}</span
						>% -
						<span class="numerical">
							{{ event.payload.transactionCommissionRange.max * 100 }}</span
						>%
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					Passive baking commission
					<template #content>
						<span class="numerical">{{
							event.payload.passiveBakingCommission * 100
						}}</span
						>%
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Passive transaction commission
					<template #content>
						<span class="numerical">{{
							event.payload.passiveTransactionCommission * 100
						}}</span
						>%
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Passive finalization commission
					<template #content>
						<span class="numerical">{{
							event.payload.passiveFinalizationCommission * 100
						}}</span
						>%
					</template>
				</DescriptionListItem>

				<DescriptionListItem>
					Min. baker stake
					<template #content>
						<Amount
							:amount="event.payload.minimumEquityCapital"
							:show-symbol="true"
						/>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Max. capital bound
					<template #content>
						<span class="numerical">{{ event.payload.capitalBound * 100 }}</span
						>%
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Max. leverage
					<template #content>
						<span class="numerical">{{
							event.payload.leverageBound.numerator /
							event.payload.leverageBound.denominator
						}}</span>
					</template>
				</DescriptionListItem>
			</DescriptionList>
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'TimeParametersChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Update time parameters to:

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Mint per payday
					<template #content>
						<span class="numerical">
							{{ event.payload.mintPerPayday }}
						</span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Reward period length
					<template #content>
						<span class="numerical">
							{{ event.payload.rewardPeriodLength }}
						</span>
						{{ event.payload.rewardPeriodLength === 1 ? 'epoch' : 'epochs' }}
					</template>
				</DescriptionListItem>
			</DescriptionList>
		</span>

		<span
			v-else-if="
				event.payload.__typename === 'MintDistributionV1ChainUpdatePayload'
			"
			class="text-theme-faded"
		>
			Update mint distribution to:

			<DescriptionList class="mt-4 ml-8">
				<DescriptionListItem>
					Baking reward account
					<template #content>
						<span class="numerical"> {{ event.payload.bakingReward }}% </span>
					</template>
				</DescriptionListItem>
				<DescriptionListItem>
					Finalization reward account
					<template #content>
						<span class="numerical">
							{{ event.payload.finalizationReward }}%
						</span>
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
import Amount from '~/components/atoms/Amount.vue'
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
