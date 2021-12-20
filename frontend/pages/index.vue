<template>
	<main class="p-4">
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Status</TableTh>
					<TableTh>Timestamp</TableTh>
					<TableTh>Hash</TableTh>
					<TableTh align="right">Transactions</TableTh>
					<TableTh>Baker</TableTh>
					<TableTh align="right">Reward (Ͼ)</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="block in blocks" :key="block.hash">
					<TableTd>
						<StatusCircle
							:class="[
								$style.statusIcon,
								{ 'text-blue-600': block.status === 'Pending' },
							]"
						/>
						{{ block.status }}
					</TableTd>
					<TableTd>{{ block.timestamp }}</TableTd>
					<TableTd :class="$style.numerical">
						<LinkButton>
							<HashtagIcon :class="$style.cellIcon" />
							{{ block.hash }}
						</LinkButton>
					</TableTd>
					<TableTd align="right" :class="$style.numerical">
						{{ block.transactions }}
					</TableTd>
					<TableTd :class="$style.numerical">
						<LinkButton>
							<UserIcon :class="$style.cellIcon" />
							{{ block.baker }}
						</LinkButton>
					</TableTd>
					<TableTd align="right" :class="$style.numerical">
						{{ block.reward }}
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</main>
</template>

<script lang="ts" setup>
import { HashtagIcon, UserIcon } from '@heroicons/vue/solid'
import { blocks } from '~/__mocks__/blocks'
</script>

<style module>
.statusIcon {
	@apply h-4 mr-2 text-green-600;
}
.cellIcon {
	@apply h-4 text-white inline align-baseline;
}

.numerical {
	@apply font-mono;
}
</style>
