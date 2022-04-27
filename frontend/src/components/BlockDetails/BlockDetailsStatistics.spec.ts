import BlockDetailsStatistics from './BlockDetailsStatistics.vue'
import { setupComponent, screen } from '~/utils/testing'
import type { BlockStatistics } from '~/types/generated'

const defaultProps = {
	blockStatistics: {
		blockTime: 8,
		finalizationTime: null,
	} as BlockStatistics,
}

const { render } = setupComponent(BlockDetailsStatistics, {
	defaultProps,
})

describe('BlockDetailsStatistics', () => {
	it('will show the formatted block time', () => {
		render({})

		expect(screen.getByRole('definition')).toHaveTextContent('8.0s')
	})

	it('will not show finalisation time if it is not available', () => {
		render({})

		expect(screen.queryByText('Finalization time')).not.toBeInTheDocument()
	})

	it('will not show finalisation time when it is available', () => {
		const props = {
			blockStatistics: {
				blockTime: 8,
				finalizationTime: 12.7,
			},
		}
		render({ props })

		expect(screen.getByText('Finalization time')).toBeInTheDocument()
		expect(screen.getByText('12.7')).toBeInTheDocument()
	})
})
