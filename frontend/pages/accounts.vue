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

<script lang="ts" setup>
import { useQuery } from '@urql/vue'

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

const data = result.data?._rawValue?.countries || result.data.countries || []
</script>
