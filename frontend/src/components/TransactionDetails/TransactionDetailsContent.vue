<template>
	<div>
		<DrawerTitle class="font-mono">
			{{ data?.transaction?.transactionHash.substring(0, 6) }}
			<Badge
				:type="data?.transaction?.result.successful ? 'success' : 'failure'"
			>
				{{ data?.transaction?.result.successful ? 'Success' : 'Rejected' }}
			</Badge>
		</DrawerTitle>
		<DrawerContent>
			<div class="grid gap-6 grid-cols-2 mb-16">
				<DetailsCard>
					<template #title>Block height / block hash</template>
					<template #default>
						{{ data?.transaction?.block.blockHeight }}
					</template>
					<template #secondary>
						{{ data?.transaction?.block.blockHash.substring(0, 6) }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="data?.transaction?.block.blockSlotTime">
					<template #title>Timestamp</template>
					<template #default>
						{{
							convertTimestampToRelative(data?.transaction?.block.blockSlotTime)
						}}
					</template>
					<template #secondary>
						{{ data?.transaction?.block.blockSlotTime }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="data?.transaction?.transactionType">
					<template #title>Transaction type / cost (Ͼ)</template>
					<template #default>
						{{ translateTransactionType(data?.transaction?.transactionType) }}
					</template>
					<template #secondary>
						{{ convertMicroCcdToCcd(data?.transaction?.ccdCost) }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="data?.transaction?.senderAccountAddress">
					<template #title>Sender</template>
					<template #default>
						<UserIcon class="h-5 inline align-baseline mr-3" />
						{{ data?.transaction?.senderAccountAddress.substring(0, 6) }}
					</template>
				</DetailsCard>
			</div>
			<Accordion>
				Events
				<span
					v-if="data?.transaction?.result.successful"
					class="text-theme-faded ml-1"
				>
					({{ data?.transaction?.result.events?.nodes.length }})
				</span>
				<template #content>
					<TransactionEventList
						v-if="data?.transaction?.result.successful"
						:events="data?.transaction.result.events?.nodes"
					/>
				</template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { UserIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Badge from '~/components/Badge.vue'
import Accordion from '~/components/Accordion.vue'
import TransactionEventList from '~/components/TransactionEventList.vue'
import {
	convertMicroCcdToCcd,
	convertTimestampToRelative,
} from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import { useTransactionQuery } from '~/queries/useTransactionQuery'

type Props = {
	id: string
}

const props = defineProps<Props>()

const { data } = await useTransactionQuery(props.id)
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-theme-interactive;
}
.cellIcon {
	@apply h-4 text-theme-white inline align-baseline;
}

.numerical {
	@apply font-mono;
	font-variant-ligatures: none;
}
</style>
