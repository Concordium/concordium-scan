/**
 * @file
 * This file contain helper functions related to finalization commissions.
 * Finalization comissions has been removed from the beginning of December 2023.
 * The frontend should only render these if they contain non zero values, since
 * the will always be zero after the tokenomic changes.
 */

import {
	FinalizationRewardsSpecialEvent,
	PaydayAccountRewardSpecialEvent,
	PaydayPoolRewardSpecialEvent,
} from '../types/generated'

export const showFinalizationReward = (
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

export const showFinalizationFromPaydayAccountReward = (
	finalizationRewards: PaydayAccountRewardSpecialEvent[]
): boolean => {
	return (
		finalizationRewards.reduce(
			(acc, current) => acc + current.finalizationReward,
			0
		) > 0
	)
}

export const showFinalizationFromPaydayPoolReward = (
	finalizationRewards: PaydayPoolRewardSpecialEvent[]
): boolean => {
	return (
		finalizationRewards.reduce(
			(acc, current) => acc + current.finalizationReward,
			0
		) > 0
	)
}
