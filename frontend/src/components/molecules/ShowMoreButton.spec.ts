import { render, screen } from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'
import ShowMoreButton from './ShowMoreButton.vue'
import { MAX_PAGE_SIZE } from '~/composables/usePagedData'

const noop = () => {
	/* noop */
}

const defaultProps = {
	newItemCount: 0,
	refetch: noop,
}

const renderComponent = (props?: RenderOptions['props']) =>
	render(ShowMoreButton, { props: { ...defaultProps, ...props } })

describe('ShowMoreButton', () => {
	it('will not show any buttons if there are no new items', () => {
		renderComponent({})

		expect(screen.queryByRole('button')).not.toBeInTheDocument()
	})

	it('will show a "show more" button if there is a new item', () => {
		renderComponent({ newItemCount: 1 })

		expect(screen.getByRole('button')).toBeInTheDocument()
		expect(screen.getByText('Show 1 more item'))
	})

	it('will show a "show more" button with plural text if there are multiple new items', () => {
		renderComponent({ newItemCount: 2 })

		expect(screen.getByRole('button')).toBeInTheDocument()
		expect(screen.getByText('Show 2 more items'))
	})

	it('will show a "refresh" button if the new item count exceeds the maximum page size', () => {
		renderComponent({ newItemCount: MAX_PAGE_SIZE + 1 })

		expect(screen.getByRole('button')).toBeInTheDocument()
		expect(
			screen.getByText(`Refresh to see more than ${MAX_PAGE_SIZE} new items`)
		)
	})

	it('will show only one button if the new item count is the same as the maximum page size', () => {
		renderComponent({ newItemCount: MAX_PAGE_SIZE })

		expect(screen.getAllByRole('button').length).toBe(1)
		expect(screen.getByText(`Show ${MAX_PAGE_SIZE} more items`))
	})
})
