import { Ref } from 'vue'
import type { PageInfo } from './generated'

export type QueryVariables = {
	after: Ref<PageInfo['endCursor']>
	before: Ref<PageInfo['endCursor']>
	first: Ref<number | undefined>
	last: Ref<number | undefined>
}
