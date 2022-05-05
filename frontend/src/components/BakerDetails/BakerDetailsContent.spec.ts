import { h } from 'vue'
import BakerDetailsContent from './BakerDetailsContent.vue'
import { setupComponent, screen, within } from '~/utils/testing'

jest.mock('~/composables/useDrawer', () => ({
	useDrawer: () => ({
		drawer: {
			push: jest.fn(),
		},
	}),
}))

jest.mock('~/composables/useDateNow', () => ({
	useDateNow: () => ({
		NOW: new Date('1970-01-01'),
	}),
}))

jest.mock('vue-router', () => ({
	useRouter: () => ({
		push: jest.fn(),
	}),
}))

// mocked as some of its imports causes problems for Jest
jest.mock(
	'~/components/molecules/ChartCards/RewardMetricsForBakerChart',
	() => ({
		render: () => h('div'),
	})
)

jest.mock('~/queries/useRewardMetricsForBakerQuery', () => ({
	useRewardMetricsForBakerQueryQuery: () => ({
		fetching: false,
		data: undefined,
	}),
}))

const defaultProps = {
	baker: {
		account: {
			address: {
				asString: 'c001-acc0un7',
			},
		},
		bakerId: 1337,
		id: '1337-acc-1d',
		state: {
			__typename: 'ActiveBakerState',
			stakedAmount: 1337420666,
			restakeEarnings: true,
		},
	},
}

const { render } = setupComponent(BakerDetailsContent, {
	defaultProps,
})

describe('BakerDetailsContent', () => {
	it('will show the account address', () => {
		render({})

		expect(screen.getByText('c001-acc0un7')).toBeInTheDocument()
	})

	it('will not show the removed details', () => {
		render({})

		expect(screen.queryByText('Removed at')).not.toBeInTheDocument()
	})

	describe('when the baker is ACTIVE', () => {
		it('will show the staked amount', () => {
			render({})

			expect(screen.getByText('1,337.420666 Ͼ')).toBeInTheDocument()
		})

		it('can show that the earnings are being restaked', () => {
			render({})

			expect(
				screen.getByText('Earnings are being restaked')
			).toBeInTheDocument()
		})

		it('can show that the earnings are not being restaked', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: { ...defaultProps.baker.state, restakeEarnings: false },
				},
			}

			render({ props })

			expect(
				screen.getByText('Earnings are not being restaked')
			).toBeInTheDocument()
		})
	})

	describe('when the baker is REMOVED', () => {
		it('will show the time it was removed', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: {
						__typename: 'RemovedBakerState',
						removedAt: '1969-07-20T20:17:40.000Z',
					},
				},
			}

			render({ props })

			expect(screen.getByText('Removed at')).toBeInTheDocument()
			expect(screen.getByText('Jul 20, 1969, 8:17 PM')).toBeInTheDocument()
		})

		it('will not show staked amount', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: {
						__typename: 'RemovedBakerState',
						removedAt: '1969-07-20T20:17:40.000Z',
					},
				},
			}

			render({ props })

			expect(screen.queryByText('Staked amount')).not.toBeInTheDocument()
		})
	})

	describe('when the baker has a pending change', () => {
		it('will show an alert', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: {
						...defaultProps.baker.state,
						pendingChange: {
							__typename: 'PendingBakerRemoval',
							effectiveTime: '1969-07-20T20:17:40.000Z',
						},
					},
				},
			}

			render({ props })

			const alert = screen.getByRole('alert')

			expect(alert).toBeVisible()
			expect(within(alert).getByRole('heading')).toHaveTextContent(
				'Pending change'
			)
		})

		it('can show if the baker is about to be removed', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: {
						...defaultProps.baker.state,
						pendingChange: {
							__typename: 'PendingBakerRemoval',
							effectiveTime: '1969-07-20T20:17:40.000Z',
						},
					},
				},
			}

			render({ props })

			expect(
				screen.getByText('Baker will be removed Jul 20, 1969, 8:17 PM')
			).toBeInTheDocument()
		})

		it('will show the new staked amount if stake is to be reduced', () => {
			const props = {
				baker: {
					...defaultProps.baker,
					state: {
						...defaultProps.baker.state,
						pendingChange: {
							__typename: 'PendingBakerReduceStake',
							effectiveTime: '1969-07-20T20:17:40.000Z',
							newStakedAmount: 421337421337,
						},
					},
				},
			}

			render({ props })

			expect(
				screen.getByText(
					'Stake will be reduced to 421,337.421337 Ͼ on Jul 20, 1969, 8:17 PM'
				)
			).toBeInTheDocument()
		})
	})
})
