<template>
	<main class="p-4">
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Fruit</TableTh>
					<TableTh>Quantity</TableTh>
					<TableTh>Price</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow>
					<TableTd>Banadna</TableTd>
					<TableTd align="right">1,337</TableTd>
					<TableTd align="right">€323.23</TableTd>
				</TableRow>
				<TableRow>
					<TableTd>Banana</TableTd>
					<TableTd align="right">1,337</TableTd>
					<TableTd align="right">€323.23</TableTd>
				</TableRow>
				<TableRow>
					<TableTd>Banana</TableTd>
					<TableTd align="right">1,337</TableTd>
					<TableTd align="right">€323.23</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</main>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import Table from '~/components/Table/Table'
import TableHead from '~/components/Table/TableHead'
import TableBody from '~/components/Table/TableBody'
import TableRow from '~/components/Table/TableRow'
import TableTh from '~/components/Table/TableTh'
import TableTd from '~/components/Table/TableTd'

export default defineComponent({
	components: {
		Table,
		TableHead,
		TableBody,
		TableRow,
		TableTd,
		TableTh,
	},
})
</script>
