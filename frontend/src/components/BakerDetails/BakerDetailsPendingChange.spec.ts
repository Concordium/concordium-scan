import BakerDetailsPendingChange from './BakerDetailsPendingChange.vue'
import { setupComponent, screen, within } from '~/utils/testing'

jest.mock('~/composables/useDateNow', () => ({
	useDateNow: () => ({
		NOW: new Date('1970-01-01'),
	}),
}))

const defaultProps = {
	pendingChange: {
		__typename: 'PendingBakerRemoval',
		effectiveTime: '1969-07-20T20:17:40.000Z',
	},
}

const { render } = setupComponent(BakerDetailsPendingChange, {
	defaultProps,
})

describe('BakerDetailsPendingChange', () => {
	it('will show an alert', () => {
		render({})

		const alert = screen.getByRole('alert')

		expect(alert).toBeVisible()
		expect(within(alert).getByRole('heading')).toHaveTextContent(
			'Pending change'
		)
	})

	// TODO: Implementation was changed without updating the tests
	//       Need to find out which implementation is correct
	// eslint-disable-next-line jest/no-disabled-tests
	it.skip('can show if the baker is about to be removed', () => {
		render({})

		expect(
			screen.getByText('Baker will be removed Jul 20, 1969, 8:17 PM')
		).toBeInTheDocument()
	})

	// eslint-disable-next-line jest/no-disabled-tests
	it.skip('will show the new staked amount if stake is to be reduced', () => {
		const props = {
			pendingChange: {
				__typename: 'PendingBakerReduceStake',
				effectiveTime: '1969-07-20T20:17:40.000Z',
				newStakedAmount: 421337421337,
			},
		}

		render({ props })

		expect(
			screen.getByText(
				'Baker stake will be reduced to 421,337.421337 Ͼ on Jul 20, 1969, 8:17 PM'
			)
		).toBeInTheDocument()
	})
})
