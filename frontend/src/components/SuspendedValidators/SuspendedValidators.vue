<template>
	<div>
		<div class="flex flex-wrap flex-grow w-1/2">
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl">
					Suspended validators ({{ suspendedValidators.length }})
				</h1>
			</div>
		</div>
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Validator ID</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow
					v-for="suspendedValidator in suspendedValidators"
					:key="suspendedValidator.id"
				>
					<TableTd>
						<BakerLink :id="suspendedValidator.id" />
					</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</div>
</template>

<script lang="ts" setup>
import type { Validators } from '~/types/generated'
import BakerLink from '~/components/molecules/BakerLink.vue'

type Props = {
	suspendedValidators: Validators[]
}
defineProps<Props>()
</script>
