import { render, screen, fireEvent } from '@testing-library/vue'
import type { RenderOptions } from '@testing-library/vue'
import Button from './Button.vue'

const slots = {
	default: 'Hello',
}

const defaultProps = {
	onClick: () => {
		/* noop */
	},
}

const renderComponent = (
	props?: RenderOptions['props'],
	attrs?: RenderOptions['attrs']
) => render(Button, { slots, props: { ...defaultProps, ...props }, attrs })

describe('Button', () => {
	it('has a label', () => {
		renderComponent()

		const button = screen.getByText(slots.default)

		expect(button).toBeInTheDocument()
	})

	it('can be clicked', () => {
		const onClick = jest.fn()

		renderComponent({
			onClick,
		})

		expect(onClick).not.toHaveBeenCalled()

		fireEvent.click(screen.getByText(slots.default))

		expect(onClick).toHaveBeenCalledTimes(1)
	})

	it('can not be clicked if button is disabled', () => {
		const onClick = jest.fn()

		renderComponent({ onClick }, { disabled: true })

		expect(onClick).not.toHaveBeenCalled()

		fireEvent.click(screen.getByText(slots.default))

		expect(onClick).not.toHaveBeenCalled()
	})

	it('can inherit aria-attributes', () => {
		const onClick = jest.fn()

		renderComponent({ onClick }, { 'aria-label': 'Click me!' })

		expect(screen.getByLabelText('Click me!')).toBeInTheDocument()
	})
})
