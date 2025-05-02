<template>
	<div>
		<div v-if="holderLoading" class="w-full h-36 text-center">
			<BWCubeLogoIcon class="w-10 h-10 animate-ping mt-8" />
		</div>
		<div v-else>
			<FtbCarousel non-carousel-classes="grid-cols-1">
				<CarouselSlide class="w-full lg:h-full">
					<Filter
						v-model="lastNTransactions"
						class="mb-4"
						:data="transactionFilterOptions"
					/>
					<Table>
						<TableHead>
							<TableRow>
								<TableTh width="20%">Transaction Hash</TableTh>
								<TableTh width="20%">Age</TableTh>
								<TableTh width="20%">From</TableTh>
								<TableTh width="20%">To</TableTh>
								<TableTh width="20%">Amount</TableTh>
								<TableTh width="20%">Value</TableTh>
							</TableRow>
						</TableHead>
						<TableBody>
							<TableRow
								v-for="(coin, index) in dataPerStablecoin?.stablecoin
									?.transactions"
								:key="index"
							>
								<TableTd>
									<TransactionLink :hash="coin.transactionHash" />
								</TableTd>
								<TableTd>
									{{ coin.dateTime ? timeAgo(coin.dateTime) : '-' }}
								</TableTd>
								<TableTd>
									<AccountLink :address="coin.from" />
								</TableTd>
								<TableTd>
									<AccountLink :address="coin.to" />
								</TableTd>
								<TableTd>
									{{ coin.amount?.toFixed(2) }}
								</TableTd>
								<TableTd>
									{{ coin.value?.toFixed(2) }}
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
import { timeAgo } from '~/utils/format'
import { useStableCoinDashboardList } from '~/queries/useStableCoinDashboardList'
import FtbCarousel from '~/components/molecules/FtbCarousel.vue'
import CarouselSlide from '~/components/molecules/CarouselSlide.vue'
import BWCubeLogoIcon from '~/components/icons/BWCubeLogoIcon.vue'
import Filter from '~/components/StableCoin/Filter.vue'
import {
	TransactionFilterOption,
	transactionFilterOptions,
} from '~/types/stable-coin'
// Define Props
const props = defineProps<{
	coinId?: string
}>()

// Loading state
const isLoading = ref(true)
const lastNTransactions = ref(TransactionFilterOption.Top20)
const limit = ref(10)

const coinId = props.coinId?.toUpperCase() ?? 'USDC'

// Fetch Data

const { data: dataPerStablecoin, fetching: holderLoading } =
	useStableCoinDashboardList(coinId, limit, lastNTransactions)

// Watch for data updates
watch(dataPerStablecoin, newData => {
	if (newData) {
		isLoading.value = false
	}
})
</script>
