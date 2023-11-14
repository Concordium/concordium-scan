<template>
	<DetailsCard>
		<template #title>APY period</template>
		<template #default>
			<div class="toggle rounded inline-block text-sm whitespace-nowrap">
				<span
					:class="
						selectedPeriod === ApyPeriod.Last7Days
							? 'selected rounded px-2 py-1 inline-block'
							: 'px-2 py-1 cursor-pointer'
					"
					@click="() => handleTogglePeriod(ApyPeriod.Last7Days)"
				>
					7 days
				</span>
				<span
					:class="
						selectedPeriod === ApyPeriod.Last30Days
							? 'selected rounded px-2 py-1 inline-block'
							: 'px-2 py-1 cursor-pointer '
					"
					@click="() => handleTogglePeriod(ApyPeriod.Last30Days)"
				>
					30 days
				</span>
			</div>
		</template>
	</DetailsCard>
	<DetailsCard>
		<template #title>Total APY ({{ periodText }})</template>
		<template #default>
			<span v-if="data && Number.isFinite(data.totalApy)" class="numerical">
				{{formatPercentage(data.totalApy!)}}%
			</span>
			<span v-else>-</span>
		</template>
	</DetailsCard>
	<DetailsCard>
		<template #title>Validator APY ({{ periodText }})</template>
		<template #default>
			<span v-if="data && Number.isFinite(data.bakerApy)" class="numerical">
				{{formatPercentage(data.bakerApy!)}}%
			</span>
			<span v-else>-</span>
		</template>
	</DetailsCard>
	<DetailsCard>
		<template #title>Delegators APY ({{ periodText }})</template>
		<template #default>
			<span
				v-if="data && Number.isFinite(data.delegatorsApy)"
				class="numerical"
			>
				{{formatPercentage(data.delegatorsApy!)}}%
			</span>
			<span v-else>-</span>
		</template>
	</DetailsCard>
</template>

<script lang="ts" setup>
import { ref, computed } from 'vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { type PoolApy, ApyPeriod } from '~/types/generated'
import { formatPercentage } from '~/utils/format'

const selectedPeriod = ref<ApyPeriod>(ApyPeriod.Last30Days)

type Props = {
	apy7days?: PoolApy
	apy30days?: PoolApy
}

const props = defineProps<Props>()

const data = computed(() =>
	selectedPeriod.value === ApyPeriod.Last7Days
		? props.apy7days
		: props.apy30days
)

const periodText = computed(() =>
	selectedPeriod.value === ApyPeriod.Last7Days ? '7 days' : '30 days'
)

const handleTogglePeriod = (period: ApyPeriod) => {
	selectedPeriod.value = period
}
</script>

<style scoped>
.commission-rates {
	background-color: var(--color-thead-bg);
}

.toggle {
	background-color: hsla(245, 24%, 90%, 5%);
	padding: 2px;
}

.selected {
	background-color: hsl(var(--color-primary));
}
</style>
