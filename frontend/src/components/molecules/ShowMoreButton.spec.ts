import ShowMoreButton from './ShowMoreButton.vue'
import { setupComponent, screen } from '~/utils/testing'
import { MAX_PAGE_SIZE } from '~/composables/usePagedData'

const noop = () => {
	/* noop */
}

const defaultProps = {
	newItemCount: 0,
	refetch: noop,
}

const { render } = setupComponent(ShowMoreButton, { defaultProps })

describe('ShowMoreButton', () => {
	it('will not show any buttons if there are no new items', () => {
		render({})

		expect(screen.queryByRole('button')).not.toBeInTheDocument()
	})

	it('will show a "show more" button if there is a new item', () => {
		const props = { newItemCount: 1 }
		render({ props })

		expect(screen.getByRole('button')).toBeInTheDocument()
		expect(screen.getByText('Show 1 more item'))
	})

	it('will show a "show more" button with plural text if there are multiple new items', () => {
		const props = { newItemCount: 2 }
		render({ props })

		expect(screen.getByRole('button')).toBeInTheDocument()
		expect(screen.getByText('Show 2 more items'))
	})

	it('will show a "refresh" button if the new item count exceeds the maximum page size', () => {
		const props = { newItemCount: MAX_PAGE_SIZE + 1 }
		render({ props })

		expect(screen.getByRole('button')).toBeInTheDocument()
		expect(
			screen.getByText(`Refresh to see more than ${MAX_PAGE_SIZE} new items`)
		)
	})

	it('will show only one button if the new item count is the same as the maximum page size', () => {
		const props = { newItemCount: MAX_PAGE_SIZE }
		render({ props })

		expect(screen.getAllByRole('button').length).toBe(1)
		expect(screen.getByText(`Show ${MAX_PAGE_SIZE} more items`))
	})
})
