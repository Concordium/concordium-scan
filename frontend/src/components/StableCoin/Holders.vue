<template>
	<div>
		<div v-if="pltHolderLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-1">
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="lastNTransactions"
						class="mb-4"
						:data="holderFilterOptions"
					/>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh width="33%">Account</TableTh>
								<TableTh width="33%">Quantity</TableTh>
								<TableTh width="1%">Percentage</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow v-for="(coin, index) in pagedData" :key="index">
								<TableTd>
									<AccountLink :address="coin.accountAddress.asString" />
								</TableTd>
								<TableTd>
									<PLtAmount
										:value="coin.amount.value"
										:decimals="Number(coin.amount.decimals)"
									/>
								</TableTd>
								<TableTd>
									<PLtAmount
										:value="
											fractionalQuantity(
												coin.amount.value,
												totalSupply
											).toString()
										"
										:decimals="2"
										:symbol="'%'"
									/>
								</TableTd>
							</TableRow>
						</TableBody>
					</Table>
				</CarouselSlide>
			</FtbCarousel>
		</div>
	</div>
</template>
<script lang="ts" setup>
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'
import { usePltTokenHolderQuery } from '~/queries/usePltTokenHolderQuery'
import type { PltaccountAmount } from '~/types/generated'
import { usePagedData } from '~/composables/usePagedData'

import {
	holderFilterOptions,
	TransactionFilterOption,
} from '~/types/stable-coin'
import { ref, watch } from 'vue'

// Define Props
const props = defineProps<{
	coinId?: string
	totalSupply?: bigint
}>()

// Loading state
const lastNTransactions = ref(TransactionFilterOption.Top20)

const coinId = props.coinId ?? ''
const totalSupply = props.totalSupply ?? BigInt(0)

const { pagedData, addPagedData } = usePagedData<PltaccountAmount>(
	[],
	lastNTransactions.value,
	lastNTransactions.value
)

const queryFirst = ref(lastNTransactions.value)

watch(
	() => lastNTransactions.value,
	newValue => {
		queryFirst.value = newValue
		pagedData.value = []
	}
)

const { data: pltHolderData, fetching: pltHolderLoading } =
	usePltTokenHolderQuery(coinId, {
		first: queryFirst,
	})

watch(
	() => pltHolderData.value,
	value => {
		if (value?.pltAccountsByTokenId) {
			addPagedData(
				value.pltAccountsByTokenId.nodes || [],
				value.pltAccountsByTokenId.pageInfo
			)
		}
	},
	{ immediate: true }
)

const fractionalQuantity = (value: string, total: bigint) => {
	// value is bigint in string format
	const valueBigInt = BigInt(value)

	if (total === 0n) return 0 // Avoid division by zero

	// Calculate percentage as (value / total) * 100
	// For better precision, multiply by 10000 first, then divide by 100 to get 2 decimal places
	const percentage = (valueBigInt * BigInt(10000)) / total
	return percentage
}
</script>
