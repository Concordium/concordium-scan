import Tooltip from './Tooltip.vue'
import { setupComponent, screen, fireEvent, waitFor } from '~/utils/testing'

const defaultProps = {
	text: 'Hello, World!',
}

const defaultSlots = {
	default: 'Hover me!',
}

const { render } = setupComponent(Tooltip, { defaultProps, defaultSlots })

describe('Tooltip', () => {
	it('will show the content', () => {
		render({})

		expect(screen.getByText(defaultSlots.default)).toBeInTheDocument()
	})

	it('will not show the tooltip by default', () => {
		render({})

		expect(screen.queryByText(defaultProps.text)).not.toBeVisible()
	})

	it('will show the tooltip when hovering the trigger element', () => {
		render({})

		fireEvent.mouseEnter(screen.getByText(defaultProps.text))

		waitFor(() => {
			expect(screen.getByText(defaultProps.text)).toBeVisible()
		})
	})

	it('will hide the tooltip when hovering off again', () => {
		render({})

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
		const props = { onMouseEnter }

		render({ props })

		expect(onMouseEnter).not.toHaveBeenCalled()

		fireEvent.mouseEnter(screen.getByText(defaultProps.text))

		waitFor(() => {
			expect(onMouseEnter).toHaveBeenCalled()
		})
	})
})
