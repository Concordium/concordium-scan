import { Ref } from 'vue'
import type { PageInfo } from './generated'

export type QueryVariables = {
	after: Ref<PageInfo['endCursor']>
	before: Ref<PageInfo['endCursor']>
	first: Ref<number | undefined> | number
	last: Ref<number | undefined>
}
