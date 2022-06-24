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

	it('can show if the baker is about to be removed', () => {
		render({})

		// Tooltip is doing us a disservice here, but this will do for now
		expect(screen.getByText('Baker will be removed in')).toBeInTheDocument()
		expect(screen.getByText('5 months')).toBeInTheDocument()
	})
})
