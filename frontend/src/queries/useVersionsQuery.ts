import { useQuery, gql } from '@urql/vue'

type VersionsResponse = {
	versions: {
		backendVersion: string
	}
}

const VersionsQuery = gql<VersionsResponse>`
	query {
		versions {
			backendVersion
		}
	}
`

export const useVersionsQuery = () => {
	const { data } = useQuery({
		context: { url: useRuntimeConfig().public.apiUrlRust },
		query: VersionsQuery,
		requestPolicy: 'cache-and-network',
	})

	return { data }
}
