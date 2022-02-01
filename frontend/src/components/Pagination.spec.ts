import { render, screen, fireEvent, within } from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'
import Pagination from './Pagination.vue'

const noop = () => {
	/* noop */
}

const defaultProps = {
	pageInfo: {
		hasNextPage: true,
		hasPreviousPage: false,
		startCursor: 'Str=',
		endCursor: 'End=',
	},
	goToPage: noop,
}

const dom = {
	FIRST_TEXT: 'First',
	PREV_TEXT: 'Previous',
	NEXT_TEXT: 'Next',
	FIRST_ARIA: 'Go to the first page',
	PREV_ARIA: 'Go to the previous page',
	NEXT_ARIA: 'Go to the next page',
}

const renderComponent = (props?: RenderOptions['props']) =>
	render(Pagination, { props: { ...defaultProps, ...props } })

describe('Pagination', () => {
	it('has three accessible navigation buttons', () => {
		renderComponent()

		expect(
			within(screen.getByLabelText(dom.FIRST_ARIA)).getByText(dom.FIRST_TEXT)
		).toBeInTheDocument()
		expect(
			within(screen.getByLabelText(dom.PREV_ARIA)).getByText(dom.PREV_TEXT)
		).toBeInTheDocument()
		expect(
			within(screen.getByLabelText(dom.NEXT_ARIA)).getByText(dom.NEXT_TEXT)
		).toBeInTheDocument()
	})

	describe('"First"-button', () => {
		it('can navigate to the first page', () => {
			const innerFn = jest.fn()
			const goToPage = jest.fn(() => innerFn)
			const pageInfo = {
				...defaultProps.pageInfo,
				hasPreviousPage: true,
			}

			const props = {
				goToPage,
				pageInfo,
			}
			renderComponent(props)

			expect(goToPage).not.toHaveBeenCalled()

			fireEvent.click(screen.getByText(dom.FIRST_TEXT))

			expect(goToPage).toHaveBeenCalledWith(pageInfo)
			expect(innerFn).toHaveBeenCalledWith('first')
		})

		it('is disabled if there are no previous pages', () => {
			renderComponent()

			expect(screen.getByLabelText(dom.FIRST_ARIA)).toBeDisabled()
		})
	})

	describe('"Previous"-button', () => {
		it('can navigate to the previous page', () => {
			const innerFn = jest.fn()
			const goToPage = jest.fn(() => innerFn)
			const pageInfo = {
				...defaultProps.pageInfo,
				hasPreviousPage: true,
			}

			const props = {
				goToPage,
				pageInfo,
			}
			renderComponent(props)

			expect(goToPage).not.toHaveBeenCalled()

			fireEvent.click(screen.getByText(dom.PREV_TEXT))

			expect(goToPage).toHaveBeenCalledWith(pageInfo)
			expect(innerFn).toHaveBeenCalledWith('previous')
		})

		it('is disabled if there are no previous pages', () => {
			renderComponent()

			expect(screen.getByLabelText(dom.PREV_ARIA)).toBeDisabled()
		})
	})

	describe('"Next"-button', () => {
		it('can navigate to the next page', () => {
			const innerFn = jest.fn()
			const goToPage = jest.fn(() => innerFn)

			const props = {
				goToPage,
			}
			renderComponent(props)

			expect(goToPage).not.toHaveBeenCalled()

			fireEvent.click(screen.getByText(dom.NEXT_TEXT))

			expect(goToPage).toHaveBeenCalledWith(defaultProps.pageInfo)
			expect(innerFn).toHaveBeenCalledWith('next')
		})

		it('is disabled if there is no next page', () => {
			const props = {
				pageInfo: {
					...defaultProps.pageInfo,
					hasNextPage: false,
				},
			}
			renderComponent(props)

			expect(screen.getByLabelText(dom.NEXT_ARIA)).toBeDisabled()
		})
	})
})
