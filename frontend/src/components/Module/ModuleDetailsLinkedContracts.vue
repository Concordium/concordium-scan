<template>
	<TableHead>
		<TableRow>
			<TableTh>Contract Address</TableTh>
			<TableTh>Age</TableTh>
		</TableRow>
	</TableHead>
	<TableBody>
		<TableRow v-for="linkedContract in linkedContracts" :key="linkedContract">
			<TableTd class="numerical">
				<ContractLink
					:address="linkedContract.contractAddress.asString"
					:contract-address-index="linkedContract.contractAddress.index"
					:contract-address-sub-index="linkedContract.contractAddress.subIndex"
				/>
			</TableTd>
			<TableTd>
				<Tooltip
					:text="convertTimestampToRelative(linkedContract.linkedDateTime, NOW)"
				>
					<DateTimeWithLineBreak :date-time="linkedContract.linkedDateTime" />
				</Tooltip>
			</TableTd>
		</TableRow>
	</TableBody>
</template>

<script lang="ts" setup>
import DateTimeWithLineBreak from '../Details/DateTimeWithLineBreak.vue'
import ContractLink from '../molecules/ContractLink.vue'
import Tooltip from '~~/src/components/atoms/Tooltip.vue'
import { LinkedContract } from '~~/src/types/generated'
import { convertTimestampToRelative } from '~~/src/utils/format'

const { NOW } = useDateNow()

type Props = {
	linkedContracts: LinkedContract[]
}
defineProps<Props>()
</script>
