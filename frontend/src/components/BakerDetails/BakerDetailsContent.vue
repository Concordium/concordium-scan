<template>
	<div>
		<BakerDetailsHeader :baker="baker" />
		<DrawerContent>
			<div class="grid gap-8 md:grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Account</template>
					<template #default>
						<AccountLink :address="baker.account.address.asString" />
					</template>
				</DetailsCard>
				<DetailsCard v-if="baker.state.__typename === 'ActiveBakerState'">
					<template #title>Staked amount</template>
					<template #default>
						<span class="numerical">
							{{ convertMicroCcdToCcd(baker.state.stakedAmount) }} Ͼ
						</span>
					</template>
					<template #secondary> {{ restakeText }} </template>
				</DetailsCard>
			</div>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { computed } from 'vue'
import BakerDetailsHeader from './BakerDetailsHeader.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import AccountLink from '~/components/molecules/AccountLink.vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import type { Baker } from '~/types/generated'

type Props = {
	baker: Baker
}

const props = defineProps<Props>()

const restakeText = computed(() =>
	props.baker.state.__typename === 'ActiveBakerState' &&
	props.baker.state.restakeEarnings
		? 'Earnings are being restaked'
		: 'Earnings are not being restaked'
)
</script>
