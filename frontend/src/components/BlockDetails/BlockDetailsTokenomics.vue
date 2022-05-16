<template>
	<div>
		<div v-if="componentState === 'loading'" class="h-48 relative">
			<Loader />
		</div>
		<NotFound v-else-if="componentState === 'empty'" class="pt-20" />
		<Error
			v-else-if="componentState === 'error'"
			:error="error"
			class="pt-20"
		/>

		<div
			v-for="event in data!.block.specialEvents?.nodes"
			v-else-if="componentState === 'success'"
			:key="event.id"
		>
			<MintDistribution
				v-if="event.__typename === 'MintSpecialEvent'"
				:data="event"
			/>

			<FinalizationRewards
				v-else-if="event.__typename === 'FinalizationRewardsSpecialEvent'"
				:data="event.finalizationRewards?.nodes || []"
				:page-info="event.finalizationRewards?.pageInfo"
				:go-to-page="goToPageFinalizationRewards"
			/>

			<BlockRewards
				v-else-if="event.__typename === 'BlockRewardsSpecialEvent'"
				:data="event"
			/>

			<BakingRewards
				v-else-if="event.__typename === 'BakingRewardsSpecialEvent'"
				:data="event"
				:page-info="event.bakingRewards?.pageInfo"
				:go-to-page="goToPageBakingRewards"
			/>
		</div>
	</div>
</template>

<script lang="ts" setup>
import { useBlockSpecialEventsQuery } from '~/queries/useBlockSpecialEventsQuery'
import { usePagination, PAGE_SIZE_SMALL } from '~/composables/usePagination'
import MintDistribution from '~/components/Tokenomics/MintDistribution.vue'
import FinalizationRewards from '~/components/Tokenomics/FinalizationRewards.vue'
import BakingRewards from '~/components/Tokenomics/BakingRewards.vue'
import BlockRewards from '~/components/Tokenomics/BlockRewards.vue'
import Error from '~/components/molecules/Error.vue'
import Loader from '~/components/molecules/Loader.vue'
import NotFound from '~/components/molecules/NotFound.vue'
import type { Block } from '~/types/generated'

// finalization rewards pagination variables
const {
	first: firstFinalizationRewards,
	last: lastFinalizationRewards,
	after: afterFinalizationRewards,
	before: beforeFinalizationRewards,
	goToPage: goToPageFinalizationRewards,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

// baking rewards pagination variables
const {
	first: firstBakingRewards,
	last: lastBakingRewards,
	after: afterBakingRewards,
	before: beforeBakingRewards,
	goToPage: goToPageBakingRewards,
} = usePagination({ pageSize: PAGE_SIZE_SMALL })

type Props = {
	blockId: Block['id']
}

const props = defineProps<Props>()

const { data, error, componentState } = useBlockSpecialEventsQuery({
	blockId: props.blockId,
	paginationVariables: {
		firstFinalizationRewards,
		lastFinalizationRewards,
		afterFinalizationRewards,
		beforeFinalizationRewards,
		firstBakingRewards,
		lastBakingRewards,
		afterBakingRewards,
		beforeBakingRewards,
	},
})
</script>
