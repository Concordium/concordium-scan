import { type Baker, BakerPoolOpenStatus } from '~/types/generated'

type BadgeType = 'success' | 'failure' | 'info'

export const composeBakerStatus = (
	baker: Baker
): [BadgeType, string] | null => {
	const { state } = baker

	if (state.__typename === 'RemovedBakerState') {
		return ['failure', 'Removed']
	}

	if (state.__typename === 'ActiveBakerState') {
		if (state.pool?.openStatus === BakerPoolOpenStatus.OpenForAll) {
			return ['success', 'Open for all']
		}

		if (state.pool?.openStatus === BakerPoolOpenStatus.ClosedForNew) {
			return ['info', 'Closed for new']
		}

		if (state.pool?.openStatus === BakerPoolOpenStatus.ClosedForAll) {
			return ['failure', 'Closed for all']
		}
	}

	return null
}
