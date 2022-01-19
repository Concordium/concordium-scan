import { Ref } from 'vue'

export type QueryVariables = {
	after: Ref<string | undefined>
	before: Ref<string | undefined>
	first: Ref<number | undefined>
	last: Ref<number | undefined>
}
