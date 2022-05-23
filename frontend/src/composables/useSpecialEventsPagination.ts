import { usePagination } from './usePagination'

export const useSpecialEventsPagination = () => {
	const {
		goToPage: goToPageMintDistribution,
		...mintDistributionPaginationVars
	} = usePagination({ pageSize: PAGE_SIZE_SMALL })

	const { goToPage: goToPageBlockRewards, ...blockRewardsPaginationVars } =
		usePagination({ pageSize: PAGE_SIZE_SMALL })

	const {
		goToPage: goToPageFinalizationRewards,
		...finalizationRewardsPaginationVars
	} = usePagination({
		pageSize: PAGE_SIZE_SMALL,
	})

	const {
		goToPage: goToSubPageFinalizationRewards,
		...finalizationRewardsSubPaginationVars
	} = usePagination({
		pageSize: PAGE_SIZE_SMALL,
	})

	const { goToPage: goToPageBakingRewards, ...bakingRewardsPaginationVars } =
		usePagination({ pageSize: PAGE_SIZE_SMALL })

	const {
		goToPage: goToSubPageBakingRewards,
		...bakingRewardsSubPaginationVars
	} = usePagination({ pageSize: PAGE_SIZE_SMALL })

	const {
		goToPage: goToPageBlockAccrueRewards,
		...blockAccrueRewardsPaginationVars
	} = usePagination({ pageSize: PAGE_SIZE_SMALL })

	const {
		goToPage: goToPagePaydayFoundationRewards,
		...paydayFoundationRewardsPaginationVars
	} = usePagination({ pageSize: PAGE_SIZE_SMALL })

	const {
		goToPage: goToPagePaydayAccountRewards,
		...paydayAccountRewardsPaginationVars
	} = usePagination({ pageSize: PAGE_SIZE_SMALL })

	const {
		goToPage: goToPagePaydayPoolRewards,
		...paydayPoolRewardsPaginationVars
	} = usePagination({ pageSize: PAGE_SIZE_SMALL })

	const paginationVariables = {
		blockRewardsPaginationVars,
		bakingRewardsPaginationVars,
		bakingRewardsSubPaginationVars,
		mintDistributionPaginationVars,
		blockAccrueRewardsPaginationVars,
		finalizationRewardsPaginationVars,
		finalizationRewardsSubPaginationVars,
		paydayFoundationRewardsPaginationVars,
		paydayAccountRewardsPaginationVars,
		paydayPoolRewardsPaginationVars,
	}

	return {
		paginationVariables,
		goToPageBlockRewards,
		goToPageBakingRewards,
		goToSubPageBakingRewards,
		goToPageMintDistribution,
		goToPageBlockAccrueRewards,
		goToPageFinalizationRewards,
		goToSubPageFinalizationRewards,
		goToPagePaydayFoundationRewards,
		goToPagePaydayAccountRewards,
		goToPagePaydayPoolRewards,
	}
}
