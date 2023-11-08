<template>
	<div>
		<div v-if="componentState === 'loading'" class="h-48 relative">
			<Loader />
		</div>
		<NotFound v-else-if="componentState === 'empty'" class="mb-8">
			No data
			<template #secondary>This block has no special events to show</template>
		</NotFound>
		<Error v-else-if="componentState === 'error'" :error="error" class="mb-8" />

		<div v-else-if="componentState === 'success' && data">
			<MintDistribution
				v-if="data.block.mintDistribution.nodes.length"
				:data="data.block.mintDistribution"
				:go-to-page="goToPageMintDistribution"
			/>

			<FinalizationRewards
				v-if="
					data.block.finalizationRewards.nodes.length &&
					showFinalizationFromFinalizationReward(
						data.block.finalizationRewards.nodes
					)
				"
				:data="data.block.finalizationRewards"
				:go-to-page="goToPageFinalizationRewards"
				:go-to-sub-page="goToSubPageFinalizationRewards"
			/>

			<BlockRewards
				v-if="data.block.blockRewards.nodes.length"
				:data="data.block.blockRewards"
				:go-to-page="goToPageBlockRewards"
			/>

			<ValidationRewards
				v-if="data.block.bakingRewards.nodes.length"
				:data="data.block.bakingRewards"
				:go-to-page="goToPageBakingRewards"
				:go-to-sub-page="goToSubPageBakingRewards"
			/>

			<BlockAccrueRewards
				v-if="data.block.blockAccruedRewards.nodes.length"
				:data="data.block.blockAccruedRewards"
				:go-to-page="goToPageBlockAccrueRewards"
			/>

			<PaydayFoundationReward
				v-if="data.block.paydayFoundationRewards.nodes.length"
				:data="data.block.paydayFoundationRewards"
				:go-to-page="goToPagePaydayFoundationRewards"
			/>

			<PaydayAccountReward
				v-if="data.block.paydayAccountRewards.nodes.length"
				:data="data.block.paydayAccountRewards"
				:go-to-page="goToPagePaydayAccountRewards"
			/>

			<PaydayPoolReward
				v-if="data.block.paydayPoolRewards.nodes.length"
				:data="data.block.paydayPoolRewards"
				:go-to-page="goToPagePaydayPoolRewards"
			/>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { useBlockSpecialEventsQuery } from '~/queries/useBlockSpecialEventsQuery'
import PaydayFoundationReward from '~/components/Tokenomics/PaydayFoundationReward.vue'
import PaydayAccountReward from '~/components/Tokenomics/PaydayAccountReward.vue'
import PaydayPoolReward from '~/components/Tokenomics/PaydayPoolReward.vue'
import MintDistribution from '~/components/Tokenomics/MintDistribution.vue'
import FinalizationRewards from '~/components/Tokenomics/FinalizationRewards.vue'
import BlockAccrueRewards from '~/components/Tokenomics/BlockAccrueRewards.vue'
import ValidationRewards from '~/components/Tokenomics/ValidationRewards.vue'
import BlockRewards from '~/components/Tokenomics/BlockRewards.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import { useSpecialEventsPagination } from '~/composables/useSpecialEventsPagination'
import type { Block } from '~/types/generated'
import { showFinalizationFromFinalizationReward } from '~~/src/utils/finalizationCommissionHelpers'

// finalization rewards pagination variables
const {
	paginationVariables,
	goToPageBlockRewards,
	goToPageBakingRewards,
	goToSubPageBakingRewards,
	goToPageMintDistribution,
	goToPageBlockAccrueRewards,
	goToPageFinalizationRewards,
	goToSubPageFinalizationRewards,
	goToPagePaydayFoundationRewards,
	goToPagePaydayAccountRewards,
	goToPagePaydayPoolRewards,
} = useSpecialEventsPagination()

type Props = {
	blockId: Block['id']
}

const props = defineProps<Props>()

const { data, error, componentState } = useBlockSpecialEventsQuery({
	blockId: props.blockId,
	paginationVariables,
})
</script>
