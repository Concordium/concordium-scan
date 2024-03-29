import NetworkSelect from './NetworkSelect.vue'
import {
	setupComponent,
	screen,
	fireEvent,
	mockLocation,
} from '~/utils/testing'

const { render } = setupComponent(NetworkSelect, {})

const MAINNET_URL = 'https://ccdscan.io/'
const TESTNET_URL = 'https://testnet.ccdscan.io/'

describe('NetworkSelect', () => {
	describe('when user is on Mainnet', () => {
		it('will render with "Mainnet" as the selected value', () => {
			const { locationCleanup } = mockLocation()

			render({})

			expect(screen.getByRole('combobox')).toHaveValue('mainnet')

			locationCleanup()
		})

		it('will redirect to the testnet URL when selecting Testnet', () => {
			const { locationAssignSpy, locationCleanup } = mockLocation()

			render({})

			fireEvent.update(screen.getByRole('combobox'), 'testnet')

			expect(locationAssignSpy).toHaveBeenCalledWith(TESTNET_URL)

			locationCleanup()
		})

		it('will show a spinner instead of a chevron when in a loading state', async () => {
			const { locationCleanup } = mockLocation()

			render({})

			expect(screen.getByTestId('network-chevron')).toBeInTheDocument()
			expect(screen.queryByTestId('spinner')).not.toBeInTheDocument()

			fireEvent.update(screen.getByRole('combobox'), 'testnet')

			const spinner = await screen.findByTestId('network-spinner')

			expect(spinner).toBeInTheDocument()
			expect(screen.queryByTestId('network-chevron')).not.toBeInTheDocument()

			locationCleanup()
		})

		it("will preserve 'www' correctly if present in URL", () => {
			const { locationAssignSpy, locationCleanup } = mockLocation({
				host: 'www.ccdscan.io',
			})

			render({})

			fireEvent.update(screen.getByRole('combobox'), 'testnet')

			expect(locationAssignSpy).toHaveBeenCalledWith(
				'https://www.testnet.ccdscan.io/'
			)

			locationCleanup()
		})
	})

	describe('when user is on Testnet', () => {
		it('will render with "Testnet" as the selected value', () => {
			const { locationCleanup } = mockLocation({
				host: 'testnet.ccdscan.io',
			})

			render({})

			expect(screen.getByRole('combobox')).toHaveValue('testnet')

			locationCleanup()
		})

		it('will redirect to the mainnet URL when selecting Mainnet', () => {
			const { locationAssignSpy, locationCleanup } = mockLocation({
				host: 'testnet.ccdscan.io',
			})

			render({})

			fireEvent.update(screen.getByRole('combobox'), 'mainnet')

			expect(locationAssignSpy).toHaveBeenCalledWith(MAINNET_URL)

			locationCleanup()
		})

		it("will preserve 'www' correctly if present in URL", () => {
			const { locationAssignSpy, locationCleanup } = mockLocation({
				host: 'www.testnet.ccdscan.io',
			})

			render({})

			fireEvent.update(screen.getByRole('combobox'), 'mainnet')

			expect(locationAssignSpy).toHaveBeenCalledWith('https://www.ccdscan.io/')

			locationCleanup()
		})
	})
})
