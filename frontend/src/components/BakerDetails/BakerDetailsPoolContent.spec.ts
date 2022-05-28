import { h } from 'vue'
import BakerDetailsPoolContent from './BakerDetailsPoolContent.vue'
import { setupComponent, screen, within } from '~/utils/testing'
import { BakerPoolOpenStatus } from '~/types/generated'

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

jest.mock('~/components/molecules/Loader', () => ({
	render: () => h('div'),
}))

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
			pool: {
				delegatedStake: 421337,
				delegatorCount: 1337,
				metadataUrl: 'https://ccdscan.io/',
				openStatus: BakerPoolOpenStatus.OpenForAll,
				totalStake: 1337420,
				commissionRates: {
					bakingCommission: 0.7,
					finalizationCommission: 4.2,
					transactionCommission: 1.0,
				},
			},
		},
	},
}

const { render } = setupComponent(BakerDetailsPoolContent, {
	defaultProps,
})

describe('BakerDetailsPoolContent', () => {
	it('will show the account address', () => {
		render({})

		expect(screen.getByText('c001-acc0un7')).toBeInTheDocument()
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
	})

	it('will show delegator count in delegator accordion', () => {
		render({})

		expect(screen.getByTestId('delegators-accordion')).toHaveTextContent(
			'Delegators (1337)'
		)
	})
})
