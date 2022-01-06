<template>
	<div>
		<DrawerTitle class="font-mono">
			{{ data?.block?.blockHash.substring(0, 6) }}
		</DrawerTitle>
		<DrawerContent>
			<div class="grid gap-6 grid-cols-2">
				<DetailsCard>
					<template #title>Timestamp</template>
					<template #default>
						{{
							convertTimestampToRelative(data?.block?.blockSlotTime || '', NOW)
						}}
					</template>
					<template #secondary>{{ data?.block?.blockSlotTime }}</template>
				</DetailsCard>
				<DetailsCard>
					<template #title>Baker id</template>
					<template #default>
						<UserIcon class="h-5 inline align-baseline mr-3" />
						{{ data?.block?.bakerId }}
					</template>
				</DetailsCard>
			</div>
		</DrawerContent>
	</div>
</template>

<script lang="ts" setup>
import { useQuery, gql } from '@urql/vue'
import { UserIcon } from '@heroicons/vue/solid'
import DrawerTitle from '~/components/Drawer/DrawerTitle.vue'
import DrawerContent from '~/components/Drawer/DrawerContent.vue'
import DetailsCard from '~/components/DetailsCard.vue'
import { convertTimestampToRelative } from '~/utils/format'

// Splitting the types out will cause an import error, as they are are not
// bundled by Nuxt. See more in README.md under "Known issues"
type Block = {
	block?: {
		id: string
		bakerId?: number
		blockHash: string
		blockHeight: number
		blockSlotTime: string
		finalized: boolean
		transactionCount: number
	}
}

type Props = {
	id: string
}

const props = defineProps<Props>()

const BlockQuery = gql<Block>`
	query ($id: ID!) {
		block(id: $id) {
			id
			blockHash
			bakerId
			blockSlotTime
		}
	}
`

const { data } = await useQuery({
	query: BlockQuery,
	variables: { id: props.id },
})

const NOW = new Date()
</script>
