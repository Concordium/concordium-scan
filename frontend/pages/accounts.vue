<template>
	<main class="p-4">
		<Table>
			<TableHead>
				<TableRow>
					<TableTh>Name</TableTh>
					<TableTh>Capital</TableTh>
					<TableTh>Currency</TableTh>
					<TableTh>Continent</TableTh>
				</TableRow>
			</TableHead>
			<TableBody>
				<TableRow v-for="country in data" :key="country.name">
					<TableTd>{{ country.name }}</TableTd>
					<TableTd>{{ country.capital }}</TableTd>
					<TableTd>{{ country.currency }}</TableTd>
					<TableTd>{{ country.continent.name }}</TableTd>
				</TableRow>
			</TableBody>
		</Table>
	</main>
</template>

<script lang="ts">
import { defineComponent } from 'vue'
import { useQuery } from '@urql/vue'
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
	setup() {
		const result = useQuery({
			query: `query {
  countries {
    name
    currency
    capital
    continent {
      name
    }
  }
}`,
		})
		return {
			data: result.data?._rawValue?.countries || result.data.countries || [],
		}
	},
})
</script>
