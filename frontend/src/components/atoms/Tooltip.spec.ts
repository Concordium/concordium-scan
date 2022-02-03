import { render, screen, waitFor } from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'
import { fireEvent } from '@testing-library/dom'
import Tooltip from './Tooltip.vue'

const defaultProps = {
	text: 'Hello, World!',
}

const defaultSlots = {
	default: 'Hover me!',
}

const renderComponent = (
	props?: RenderOptions['props'],
	slots?: RenderOptions['slots']
) =>
	render(Tooltip, {
		props: { ...defaultProps, ...props },
		slots: { ...defaultSlots, ...slots },
	})

describe('Tooltip', () => {
	it('will show the content', () => {
		renderComponent()

		expect(screen.getByText(defaultSlots.default)).toBeInTheDocument()
	})

	it('will not show the tooltip by default', () => {
		renderComponent()

		expect(screen.queryByText(defaultProps.text)).not.toBeVisible()
	})

	it('will show the tooltip when hovering the trigger element', () => {
		renderComponent()

		fireEvent.mouseEnter(screen.getByText(defaultProps.text))

		waitFor(() => {
			expect(screen.getByText(defaultProps.text)).toBeVisible()
		})
	})

	it('will hide the tooltip when hovering off again', () => {
		renderComponent()

		fireEvent.mouseEnter(screen.getByText(defaultProps.text))

		waitFor(() => {
			// this assertion is needed to avoid a false positive,
			// as the tooltip is not visible until the animation completes
			expect(screen.getByText(defaultProps.text)).toBeVisible()
		})

		fireEvent.mouseLeave(screen.getByText(defaultProps.text))

		waitFor(() => {
			expect(screen.getByText(defaultProps.text)).not.toBeVisible()
		})
	})

	it('will execute mouseenter handler prop when hovered', () => {
		const onMouseEnter = jest.fn()
		renderComponent({ onMouseEnter })

		expect(onMouseEnter).not.toHaveBeenCalled()

		fireEvent.mouseEnter(screen.getByText(defaultProps.text))

		waitFor(() => {
			expect(onMouseEnter).toHaveBeenCalled()
		})
	})
})
