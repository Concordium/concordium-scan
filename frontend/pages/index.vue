<template>
	<main class="p-4">
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Status</TableTh>
					<TableTh>Timestamp</TableTh>
					<TableTh>Hash</TableTh>
					<TableTh>Transactions</TableTh>
					<TableTh>Baker</TableTh>
					<TableTh>Reward (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="block in blocks" :key="block.hash">
					<TableTd
						><StatusCircle
							:class="[
								$style.statusIcon,
								{ 'text-blue-600': block.status === 'Pending' },
							]"
						/>{{ block.status }}</TableTd
					>
					<TableTd>{{ block.timestamp }}</TableTd>
					<TableTd :class="$style.numerical"
						><LinkButton
							><HashtagIcon :class="$style.cellIcon" />{{
								block.hash
							}}</LinkButton
						></TableTd
					>
					<TableTd align="right" :class="$style.numerical">{{
						block.transactions
					}}</TableTd>
					<TableTd :class="$style.numerical"
						><LinkButton
							><UserIcon :class="$style.cellIcon" />{{
								block.baker
							}}</LinkButton
						></TableTd
					>
					<TableTd align="right" :class="$style.numerical">{{
						block.reward
					}}</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</main>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import { blocks } from '~/__mocks__/blocks'
import StatusCircle from '~/components/icons/StatusCircle'
import LinkButton from '~/components/atoms/LinkButton'
import Table from '~/components/Table/Table'
import TableHead from '~/components/Table/TableHead'
import TableBody from '~/components/Table/TableBody'
import TableRow from '~/components/Table/TableRow'
import TableTh from '~/components/Table/TableTh'
import TableTd from '~/components/Table/TableTd'

export default defineComponent({
	components: {
		StatusCircle,
		LinkButton,
		Table,
		TableHead,
		TableBody,
		TableRow,
		TableTd,
		TableTh,
		HashtagIcon,
		UserIcon,
	},
	data() {
		return {
			blocks,
		}
	},
})
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-green-600;
}
.cellIcon {
	@apply h-4 text-white inline align-baseline mr-1;
}

.numerical {
	@apply font-mono;
}
</style>
