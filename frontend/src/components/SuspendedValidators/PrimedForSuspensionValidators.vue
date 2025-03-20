<template>
	<div>
		<div class="flex flex-wrap flex-grow w-1/2">
			<div class="w-full flex items-center justify-items-stretch">
				<h1 class="inline-block text-2xl">
					Primed for suspension validators ({{
						primedForSuspensionValidators.length
					}})
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
					v-for="primedForSuspensionValidator in primedForSuspensionValidators"
					:key="primedForSuspensionValidator.id"
				>
					<TableTd>
						<BakerLink :id="primedForSuspensionValidator.id" />
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
	primedForSuspensionValidators: Validators[]
}
defineProps<Props>()
</script>
