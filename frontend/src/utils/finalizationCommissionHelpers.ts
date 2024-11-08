/**
 * @file
 * This file contain helper functions related to finalization commissions.
 * Finalization commissions has been removed from the beginning of December 2023.
 * The frontend should only render these if they contain non zero values, since
 * the will always be zero after the tokenomic changes.
 */

import type {
	FinalizationRewardsSpecialEvent,
	Scalars,
} from '../types/generated'

interface FinalizationReward {
	finalizationReward: Scalars['UnsignedLong']
}

export const showFinalizationFromFinalizationReward = (
	finalizationRewards: FinalizationRewardsSpecialEvent[]
): boolean => {
	return (
		finalizationRewards.reduce((acc, current) => {
			return (
				acc +
				(current.finalizationRewards?.nodes?.reduce(
					(acci, currenti) => acci + currenti.amount,
					0
				) ?? 0)
			)
		}, 0) > 0
	)
}

export const showFinalizationFromReward = (
	finalizationRewards: FinalizationReward[]
): boolean => {
	return (
		finalizationRewards.reduce(
			(acc, current) => acc + current.finalizationReward,
			0
		) > 0
	)
}
