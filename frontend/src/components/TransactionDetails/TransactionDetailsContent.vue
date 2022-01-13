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
						{{ data?.transaction?.blockHeight }}
					</template>
					<template #secondary>
						{{ data?.transaction?.blockHash.substring(0, 6) }}
					</template>
				</DetailsCard>
				<DetailsCard v-if="data?.transaction?.transactionType">
					<template #title>Transaction type</template>
					<template #default>
						{{ translateTransactionType(data?.transaction?.transactionType) }}
					</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Cost (Ͼ)</template>
					<template #default>
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
				<template #content> Tx events go here </template>
			</Accordion>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { useQuery, gql } from '@urql/vue'
import { UserIcon } from '@heroicons/vue/solid/index.js'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import Badge from '~/components/Badge.vue'
import Accordion from '~/components/Accordion.vue'
import { convertMicroCcdToCcd } from '~/utils/format'
import { translateTransactionType } from '~/utils/translateTransactionTypes'
import type { Transaction } from '~/types/transactions'

type TxResponse = {
	transaction?: Transaction
}

type Props = {
	id: string
}

const props = defineProps<Props>()

const TxQuery = gql<TxResponse>`
	query ($id: ID!) {
		transaction(id: $id) {
			ccdCost
			blockHash
			blockHeight
			transactionHash
			senderAccountAddress
			result {
				successful
			}
			transactionType {
				__typename
				... on AccountTransaction {
					accountTransactionType
				}
				... on CredentialDeploymentTransaction {
					credentialDeploymentTransactionType
				}
				... on UpdateTransaction {
					updateTransactionType
				}
			}
		}
	}
`

const { data } = await useQuery({
	query: TxQuery,
	variables: { id: props.id },
})
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
