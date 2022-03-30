import ChainUpdateEnqueued from './ChainUpdateEnqueued.vue'
import { setupComponent, screen } from '~/utils/testing'
import type { ChainUpdateEnqueued as ChainUpdateEnqueuedType } from '~/types/generated'

const defaultProps = {
	event: {
		__typename: 'ChainUpdateEnqueued',
		effectiveTime: '1969-07-20T20:17:40.000Z',
		payload: {
			__typename: 'RootKeysChainUpdatePayload',
		},
	} as ChainUpdateEnqueuedType,
}

const { render } = setupComponent(ChainUpdateEnqueued, { defaultProps })

describe('ChainUpdateEnqueued', () => {
	it('will show a chain update with a formatted timestamp', () => {
		render({})

		expect(
			screen.getByText(
				'Chain update enqueued effective at Jul 20, 1969, 8:17 PM'
			)
		).toBeInTheDocument()
	})

	it('MicroCcdPerEuroChainUpdatePayload: will show a new exchange rate', () => {
		const props = {
			event: {
				...defaultProps.event,
				payload: {
					__typename: 'MicroCcdPerEuroChainUpdatePayload',
					exchangeRate: {
						numerator: 13371337,
						denominator: 1,
					},
				},
			} as ChainUpdateEnqueuedType,
		}
		render({ props })

		expect(
			screen.getByText('The CCD/EUR exchange rate was updated to 13.371337')
		).toBeInTheDocument()
	})
})
